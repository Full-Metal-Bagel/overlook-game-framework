using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using Overlook.Pool;
using Unity.Collections;
using UnityEngine;

#if OVERLOOK_ECS_USE_UNITY_COLLECTION
using TMask = Overlook.Ecs.NativeBitArrayMask;
using TSet = Overlook.Ecs.NativeBitArraySet;
#else
using TMask = Overlook.Ecs.Mask;
using TSet = Overlook.Ecs.SortedSetTypeSet;
#endif

namespace Overlook.Ecs;

public readonly record struct UntypedComponent(Array Storage, int Row, StorageType Type);

[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
public sealed class Archetypes : IDisposable
{
    internal readonly Pool<EntityMeta> _meta = new(512, EntityMeta.Invalid);
    internal readonly List<Table> _tables = new();
    private readonly Dictionary<TSet, Table> _typeTableMap = new();
    // TODO: profile
    internal readonly Dictionary<TMask, Query> _queries = new();
    internal readonly Dictionary<StorageType, List<Table>> _tablesByType = new();
    private static readonly StorageType s_entityType = StorageType.Create<Entity>();

    private readonly HashSet<object> _pooledInstances = new(1024, ReferenceEqualityComparer<object>.Default);
    // TODO: concurrent?
    private Dictionary<StorageType, List<object>>?[] _objectStorages =
        new Dictionary<StorageType, List<object>>?[TypeIdAssigner.MaxTypeCapacity];

    private bool _isDisposed = false;

    private readonly TypeObjectPoolCache _poolsCache = new();

    public Archetypes()
    {
        var types = TSet.Create(s_entityType);
        AddTable(types, new TableStorage(types));
    }

    public Entity Use(Identity identity)
    {
        if (_meta.Use(identity)) return Spawn(identity);
        throw new ArgumentException($"the entity {identity} is already existing", nameof(identity));
    }

    public Entity Spawn()
    {
        var identity = _meta.Add(EntityMeta.Invalid);
        return Spawn(identity);
    }

    private Entity Spawn(Identity identity)
    {
        var table = _tables[0];
        int row = table.Add(identity);
        _meta[identity] = new EntityMeta(table.Id, row);
        var entity = new Entity(identity);
        if (identity.Index >= _objectStorages.Length)
        {
            Array.Resize(ref _objectStorages, _objectStorages.Length << 1);
        }
        _objectStorages[identity.Index] ??= new Dictionary<StorageType, List<object>>();
        table.GetStorage<Entity>()[row] = entity;
        return entity;
    }

    public bool Despawn(Identity identity)
    {
        if (!IsAlive(identity)) return false;

        var meta = _meta[identity];
        var table = _tables[meta.TableId];
        table.Remove(meta.Row);
        var refStorage = _objectStorages[identity.Index]!;
        foreach (var objectComponents in refStorage.Values)
        {
            ReturnComponents(objectComponents);
        }
        refStorage.Clear();
        _meta.Remove(identity);
        return true;
    }

    public void BuildComponents<TBuilder>(Identity identity, TBuilder builder) where TBuilder : IComponentsBuilder
    {
        ThrowIfNotAlive(identity);
        using var types = new PooledList<StorageType>(32);
        builder.CollectTypes(types.Value);
        if (types.Value.Count == 0) return;
        AddComponentTypes(identity, types.Value);
        var referenceInstancesStorage = _objectStorages[identity.Index]!;
        foreach (var type in types.Value)
        {
            if (!type.IsValueType && !referenceInstancesStorage.ContainsKey(type))
            {
                referenceInstancesStorage.Add(type, RentComponents());
            }
        }
        builder.Build(new ArchetypesBuilder(this), identity);
        WarningIfEmptyObject(identity, types);
    }

    public void AddComponent<T>(Identity identity, T data) where T : unmanaged
    {
        ThrowIfNotAlive(identity);
        var type = StorageType.Create<T>();
        WarningIfOverwriteComponent(identity, type);
        var (table, row) = AddComponentType(identity, type);
        if (!type.IsTag)
        {
            var storage = table.GetStorage<T>();
            storage[row] = data;
        }
    }

    public void AddDefaultComponent(Identity identity, Type componentType)
    {
        ThrowIfNotAlive(identity);
        var type = StorageType.Create(componentType);
        AddComponentType(identity, type);
    }

    public void AddUntypedValueComponent(Identity identity, object data)
    {
        ThrowIfNotAlive(identity);
        var type = StorageType.Create(data.GetType());
        WarningIfOverwriteComponent(identity, type);
        var (table, row) = AddComponentType(identity, type);
        if (!type.IsTag)
        {
            var storage = table.GetStorage(type);
            storage.SetValue(data, row);
        }
    }

    public T AddObjectComponent<T>(Identity identity) where T : class, new()
    {
        var instance = _poolsCache.GetPool<T>().Rent();
        _pooledInstances.Add(instance);
        AddObjectComponent(identity, instance);
        return instance;
    }

    public T AddObjectComponent<T>(Identity identity, T data) where T : class
    {
        ThrowIfNotAlive(identity);
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (data.GetType().IsValueType)
        {
            AddUntypedValueComponent(identity, data);
            return data;
        }

        var components = GetOrCreateComponentsStorage(identity, data.GetType());
        if (components.Count == 0) components.Add(data);
        else Debug.LogError($"there's existing type of {data.GetType()}, use `{nameof(AddMultipleObjectComponent)}` to add multiple component with same type onto the entity.", data);
        return data;
    }

    internal void CreateObjectComponentWithoutTableChanges<T>(Identity identity, bool allowDuplicated = false) where T : class, new()
    {
        var instance = _poolsCache.GetPool<T>().Rent();
        AddObjectComponentWithoutTableChanges(identity, instance, allowDuplicated);
    }

    internal void AddObjectComponentWithoutTableChanges<T>(Identity identity, T instance, bool allowDuplicated = false) where T : class
    {
        ThrowIfNotAlive(identity);
        var type = StorageType.Create(instance.GetType());
        var components = GetOrCreateComponentsStorage(identity, instance.GetType());
        if (components.Count >= 1 && !allowDuplicated)
        {
            Debug.LogError($"there's existing type of {type.Type}, set `{nameof(allowDuplicated)}` = `true` to add multiple component with same type onto the entity.", instance as UnityEngine.Object);
            return;
        }
        components.Add(instance);
    }

    public IReadOnlyList<object> GetObjectComponents(Identity identity, StorageType type)
    {
        return _objectStorages[identity.Index]![type];
    }

    public EntityObjectComponents GetObjectComponents(Identity identity)
    {
        ThrowIfNotAlive(identity);
        return new EntityObjectComponents(_objectStorages[identity.Index]!);
    }

    public T AddMultipleObjectComponent<T>(Identity identity) where T : class, new()
    {
        var instance = _poolsCache.GetPool<T>().Rent();
        _pooledInstances.Add(instance);
        AddMultipleObjectComponent(identity, instance);
        return instance;
    }

    public T AddMultipleObjectComponent<T>(Identity identity, T data) where T : class
    {
        ThrowIfNotAlive(identity);
        if (data == null) throw new ArgumentNullException(nameof(data));
        var storage = GetOrCreateComponentsStorage(identity, data.GetType());
        if (!storage.Contains(data, ReferenceEqualityComparer.Default)) storage.Add(data);
        return data;
    }

    private List<object> GetOrCreateComponentsStorage(Identity identity, Type dataType)
    {
        ThrowIfNotAlive(identity);
        WarningIfTagClass(dataType);
        var type = StorageType.Create(dataType);
        var refStorage = _objectStorages[identity.Index]!;
        if (!refStorage.TryGetValue(type, out var components))
        {
            AddComponentType(identity, type);
            components = RentComponents();
            refStorage[type] = components;
        }
        return components;
    }

    public void RemoveObjectComponent<T>(Identity identity, T instance) where T : class
    {
        ThrowIfNotAlive(identity);
        var type = StorageType.Create(instance.GetType());
        var refStorage = _objectStorages[identity.Index]!;
        if (refStorage.TryGetValue(type, out var components))
        {
            components.Remove(instance);
            if (components.Count == 0) RemoveComponent(identity, instance.GetType());
        }
    }

    public void RemoveObjectComponent<T>(Identity identity) where T : class
    {
        ThrowIfNotAlive(identity);
        RemoveComponentType(identity, StorageType.Create<T>());
        var refStorage = _objectStorages[identity.Index]!;
        if (refStorage.Remove(StorageType.Create<T>(), out var components))
        {
            ReturnComponents(components);
        }
    }

    public void RemoveComponent<T>(Identity identity) where T : unmanaged
    {
        ThrowIfNotAlive(identity);
        RemoveComponentType(identity, StorageType.Create<T>());
    }

    public void RemoveComponent(Identity identity, Type type)
    {
        ThrowIfNotAlive(identity);
        var storageType = StorageType.Create(type);
        RemoveComponentType(identity, storageType);
        var refStorage = _objectStorages[identity.Index]!;
        if (refStorage.Remove(storageType, out var components))
        {
            ReturnComponents(components);
        }
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    private List<object> RentComponents()
    {
        return _poolsCache.GetPool<List<object>>().Rent();
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    private void ReturnComponents(List<object> components)
    {
        foreach (var obj in components)
        {
            if (_pooledInstances.Remove(obj))
            {
                _poolsCache.GetPool(obj.GetType()).Recycle(obj);
            }
        }

        _poolsCache.GetPool<List<object>>().Recycle(components);
    }

    internal (Table table, int row) AddComponentTypes<TCollection>(Identity identity, TCollection types)
        where TCollection : IReadOnlyList<StorageType>
    {
        ThrowIfNotAlive(identity);
        var meta = _meta[identity];
        var oldTable = _tables[meta.TableId];

        using var newTypes = TSet.Create(oldTable.Types, Allocator.Temp);
        bool hasNewValueType = false;
        for (int i = 0; i < types.Count; i++)
        {
            var type = types[i];
            RecursiveAddTypeAndRelatedGroupTypes(type);
        }

        if (!_typeTableMap.TryGetValue(newTypes, out var newTable))
        {
            var persistentNewTypes = TSet.Create(newTypes);
            var storage = hasNewValueType ? new TableStorage(persistentNewTypes) : oldTable.TableStorage;
            newTable = AddTable(persistentNewTypes, storage);
        }

        int newRow = Table.MoveEntry(identity, meta.Row, oldTable, newTable);
        _meta[identity] = new EntityMeta(newTable.Id, newRow);
        return (newTable, newRow);

        void RecursiveAddTypeAndRelatedGroupTypes(StorageType type, bool newInstance = false)
        {
            if (newTypes.Contains(type)) return;
            newTypes.Add(type);
            WarningIfCanBeUnmanagedAndThrowIfValueTypeIsNotUnmanaged(type.Type);
            hasNewValueType = hasNewValueType || type is { IsValueType: true, IsTag: false };
            if (type is { IsValueType: false } && newInstance)
            {
                WarningIfTagClass(type.Type);
                var refStorage = _objectStorages[identity.Index]!;
                if (!refStorage.TryGetValue(type, out var components))
                {
                    components = RentComponents();
                    refStorage[type] = components;
                }

                if (components.Count == 0)
                {
                    var instance = _poolsCache.GetPool(type.Type).Rent();
                    _pooledInstances.Add(instance);
                    components.Add(instance);
                }
            }
            if (!ComponentGroups.Groups.TryGetValue(type.Type, out var group)) return;
            foreach (var (memberType, createInstance) in group) RecursiveAddTypeAndRelatedGroupTypes(StorageType.Create(memberType), createInstance);
        }
    }

    private (Table table, int row) AddComponentType(Identity identity, StorageType type)
    {
        ThrowIfNotAlive(identity);
        using var types = new PooledList<StorageType>(1);
        types.Value.Add(type);
        return AddComponentTypes(identity, types.Value);
    }

    private void RemoveComponentType(Identity identity, StorageType type)
    {
        ThrowIfNotAlive(identity);
        var meta = _meta[identity];
        var oldTable = _tables[meta.TableId];

        if (!oldTable.Types.Contains(type))
        {
            throw new ArgumentException($"cannot remove non-existent component {type.Type.Name} from entity {identity}", nameof(type));
        }

        using var newTypes = TSet.Create(oldTable.Types, Allocator.Temp);
        newTypes.Remove(type);

        if (!_typeTableMap.TryGetValue(newTypes, out var newTable))
        {
            var persistentNewTypes = TSet.Create(newTypes);
            var storage = type.IsValueType ? new TableStorage(persistentNewTypes) : oldTable.TableStorage;
            newTable = AddTable(persistentNewTypes, storage);
        }

        int newRow = Table.MoveEntry(identity, meta.Row, oldTable, newTable);
        _meta[identity] = new EntityMeta(newTable.Id, newRow);
    }

    public unsafe Span<byte> GetComponentRawData(Identity identity, StorageType type)
    {
        ThrowIfNotAlive(identity);
        Debug.Assert(type.IsUnmanagedType, $"Type {type.Type.Name} must be unmanaged to get raw data");
        if (type.IsTag) return Span<byte>.Empty;

        var meta = _meta[identity];
        var table = _tables[meta.TableId];
        var storage = table.GetStorage(type);
        Debug.Assert(storage.GetType().GetElementType() == type.Type, $"Storage type mismatch. Expected: {type.Type.Name}, Got: {storage.GetType().GetElementType()?.Name}");
        var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(storage, meta.Row);
        var size = type.UnmanagedTypeSize;
        return new Span<byte>(ptr.ToPointer(), size);
    }

    public void SetComponentRawData(Identity identity, StorageType type, ReadOnlySpan<byte> data)
    {
        ThrowIfNotAlive(identity);
        var component = GetComponentRawData(identity, type);
        Debug.Assert(data.Length == component.Length, $"Raw data length mismatch for component {type.Type.Name} on entity {identity}. Expected: {component.Length} bytes, Got: {data.Length} bytes");
        data.CopyTo(component);
    }

    public ref T GetComponent<T>(Identity identity) where T : unmanaged
    {
        ThrowIfNotAlive(identity);
        Debug.Assert(!StorageType.Create<T>().IsTag, $"Cannot get reference to tag component of type {typeof(T).Name}");
        var meta = _meta[identity];
        var table = _tables[meta.TableId];
        return ref table.GetStorage<T>()[meta.Row];
    }

    public object GetBoxedValueComponent(Identity identity, Type type)
    {
        ThrowIfNotAlive(identity);
        var storageType = StorageType.Create(type);
        Debug.Assert(storageType.IsValueType, $"Type {type.Name} must be a value type to be boxed");
        Debug.LogWarning($"boxing value type {type}");
        if (storageType.IsTag) return null!;

        var meta = _meta[identity];
        var table = _tables[meta.TableId];
        return table.GetStorage(storageType).GetValue(meta.Row);
    }

    public object GetObjectComponent(Identity identity, Type type)
    {
        ThrowIfNotAlive(identity);
        TryGetObjectComponent(identity, type, out object? component);
        Debug.Assert(component != null, $"Object component of type {type.Name} not found on entity {identity}");
        return component!;
    }

    public bool HasComponent(StorageType type, Identity identity)
    {
        ThrowIfNotAlive(identity);
        WarnSystemType(type.Type);
        var meta = _meta[identity];
        return _tables[meta.TableId].TypesInHierarchy.Contains(type);
    }

    public T GetObjectComponent<T>(Identity identity) where T : class
    {
        ThrowIfNotAlive(identity);
        TryGetObjectComponent(identity, out T? component);
        Debug.Assert(component != null, $"Object component of type {typeof(T).Name} not found on entity {identity}");
        return component!;
    }

    public bool TryGetObjectComponent(Identity identity, Type type, out object? component)
    {
        ThrowIfNotAlive(identity);
        var entityComponents = _objectStorages[identity.Index]!;
        var hasComponents = entityComponents.TryGetValue(StorageType.Create(type), out List<object>? value);
        if (hasComponents)
        {
            component = value!.LastOrDefault();
            return component != null;
        }
        // TODO: cache type hierarchy tree for optimization
        foreach (var (t, v) in entityComponents)
        {
            if (type.IsAssignableFrom(t.Type))
            {
                component = v[^1];
                return true;
            }
        }
        component = null;
        return false;
    }

    public bool TryGetObjectComponent<T>(Identity identity, out T? component) where T : class
    {
        ThrowIfNotAlive(identity);
        var has = TryGetObjectComponent(identity, typeof(T), out object? c);
        component = (T?)c;
        return has;
    }

    internal Query GetQuery(TMask mask, Func<Archetypes, TMask, List<Table>, Query> createQuery)
    {
        if (_queries.TryGetValue(mask, out var query))
        {
            mask.Dispose();
            return query;
        }

        var matchingTables = new List<Table>();

        var type = mask.FirstType;
        if (!_tablesByType.TryGetValue(type, out var typeTables))
        {
            typeTables = new List<Table>();
            _tablesByType[type] = typeTables;
        }

        foreach (var table in typeTables)
        {
            if (!IsMaskCompatibleWith(mask, table)) continue;

            matchingTables.Add(table);
        }

        query = createQuery(this, mask, matchingTables);
        _queries.Add(mask, query);
        return query;
    }

    internal static bool IsMaskCompatibleWith(TMask mask, Table table)
    {
        return mask.IsMaskCompatibleWith(table.TypesInHierarchy);
    }

    internal bool IsAlive(Identity identity) => _meta.IsAlive(identity);

    internal EntityMeta GetEntityMeta(in Identity identity) => _meta[identity];

    internal Table GetTable(int tableId)
    {
        return _tables[tableId];
    }

    internal TSet GetTableTypes(Identity identity)
    {
        ThrowIfNotAlive(identity);
        return GetTable(GetEntityMeta(identity).TableId).Types;
    }

    internal void GetAllValueComponents(Identity identity, ICollection<UntypedComponent> components)
    {
        ThrowIfNotAlive(identity);
        var meta = _meta[identity];
        var table = _tables[meta.TableId];
        foreach (var (type, storage) in table.TableStorage.Storages)
        {
            components.Add(new UntypedComponent { Storage = storage, Row = meta.Row, Type = type });
        }
    }

    private Table AddTable(TSet types, TableStorage storage)
    {
        var table = new Table(_tables.Count, types, storage);
        _tables.Add(table);
        _typeTableMap.Add(types, table);

        foreach (var type in table.TypesInHierarchy)
        {
            if (!_tablesByType.TryGetValue(type, out var tableList))
            {
                tableList = new List<Table>();
                _tablesByType[type] = tableList;
            }

            tableList.Add(table);
        }

        foreach (var query in _queries.Values.Where(query => IsMaskCompatibleWith(query.Mask, table)))
        {
            query.AddTable(table);
        }

        return table;
    }

    internal void FindObjectComponents<T>(Identity identity, ICollection<T> collection) where T : class
    {
        ThrowIfNotAlive(identity);
        foreach (var (key, value) in _objectStorages[identity.Index]!)
        {
            if (!typeof(T).IsAssignableFrom(key.Type)) continue;
            foreach (object? obj in value) collection.Add((T)obj);
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        for (int index = 0; index < _objectStorages.Length; index++)
        {
            var refStorage = _objectStorages[index];
            if (refStorage == null)
            {
                continue;
            }

            foreach (List<object> components in refStorage.Values)
            {
                _poolsCache.GetPool<List<object>>().Recycle(components);
            }

            _objectStorages[index] = null;
        }

        foreach (var instance in _pooledInstances)
        {
            _poolsCache.GetPool(instance.GetType()).Recycle(instance);
        }
        _pooledInstances.Clear();
        _poolsCache.Dispose();

        foreach (var table in _tables) table.Dispose();
        foreach (var query in _queries.Values) query.Mask.Dispose();
    }

    [Conditional("OVERLOOK_ECS_DEBUG")]
    [SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
    private static void WarnSystemType(Type type)
    {
        // HACK: system interfaces had been skipped
        if (!string.IsNullOrEmpty(type.Namespace) && type.Namespace.StartsWith("System.", StringComparison.Ordinal))
        {
            Debug.LogWarning("don't use system interface as component of entity, they had been skipped for performance reason");
        }
    }

    private static readonly HashSet<Type> s_checkedTypes = new();

    [Conditional("OVERLOOK_ECS_DEBUG")]
    [SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
    private static void WarningIfCanBeUnmanagedAndThrowIfValueTypeIsNotUnmanaged(Type type)
    {
        if (!s_checkedTypes.Add(type)) return;
        if (type.IsClass)
        {
            if (type.BaseType != typeof(object)) return;
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fields.Any(fi => !fi.FieldType.IsUnmanaged())) return;
            Debug.LogWarning($"{type} can be changed to `struct` for performance gain");
        }
        if (type.IsValueType && !type.IsUnmanaged())
        {
            throw new ArgumentException($"ValueType {type} should be unmanaged", nameof(type));
        }
    }

    [Conditional("OVERLOOK_ECS_DEBUG")]
    [SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
    private static void WarningIfTagClass(Type type)
    {
        if (!type.IsValueType && StorageType.Create(type).IsTag)
            Debug.LogWarning($"{type} can be changed to `struct` tag");
    }

    [Conditional("OVERLOOK_ECS_DEBUG")]
    [SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
    void WarningIfEmptyObject(Identity entity, List<StorageType> types)
    {
        var refStorage = _objectStorages[entity.Index]!;
        refStorage.TryGetValue(StorageType.Create<GameObject>(), out var unityObjects);
        var unityObject = unityObjects?.FirstOrDefault() as GameObject;
        var unityObjectName = unityObject == null ? entity.ToString() : unityObject.name;
        foreach (var type in types)
        {
            if (type.IsValueType) continue;

            if (refStorage.TryGetValue(type, out var objects))
            {
                if (objects.Count == 0)
                    Debug.LogError($"Entity {unityObjectName} has empty component {type}", unityObject);
            }
            else
            {
                Debug.LogError($"Entity {unityObjectName} has no list of component {type}", unityObject);
            }
        }
    }

    [Conditional("OVERLOOK_ECS_DEBUG")]
    [SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
    void WarningIfOverwriteComponent(Identity identity, StorageType type)
    {
        if (HasComponent(type, identity))
            Debug.LogWarning($"overwrite component: {identity}.{type.Type.Name}");
    }

    void ThrowIfNotAlive(Identity identity)
    {
        if (!IsAlive(identity))
        {
            throw new ArgumentException($"entity {identity} is already despawned", nameof(identity));
        }
    }

    public readonly struct EntityObjectComponents
#if OVERLOOK_ECS_USE_LINQGEN
            : Cathei.LinqGen.IStructEnumerable<(StorageType type, IReadOnlyList<object> components), EntityObjectComponents.Enumerator>
#endif
    {
        private readonly Dictionary<StorageType, List<object>> _components;
        public Enumerator GetEnumerator() => new(_components);

        public EntityObjectComponents(Dictionary<StorageType, List<object>> components)
        {
            _components = components;
        }

        public struct Enumerator : IEnumerator<(StorageType type, IReadOnlyList<object> components)>
        {
            private Dictionary<StorageType, List<object>>.Enumerator _enumerator;

            public Enumerator(Dictionary<StorageType, List<object>> components)
            {
                _enumerator = components.GetEnumerator();
            }

            public bool MoveNext() => _enumerator.MoveNext();
            public void Reset() => ((IEnumerator)_enumerator).Reset();
            public (StorageType type, IReadOnlyList<object> components) Current => (_enumerator.Current.Key, _enumerator.Current.Value);
            object IEnumerator.Current => Current;
            public void Dispose() => _enumerator.Dispose();
        }
    }
}
