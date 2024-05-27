using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Game;
using KG;
using Unity.Collections;
using Debug = Game.Debug;

#if ARCHETYPE_USE_NATIVE_BIT_ARRAY
using TMask = RelEcs.NativeBitArrayMask;
using TSet = RelEcs.NativeBitArraySet;
#else
using TMask = RelEcs.Mask;
using TSet = RelEcs.SortedSetTypeSet;
#endif

namespace RelEcs
{
    public readonly struct UntypedComponent
    {
        public Array Storage { get; init; }
        public int Row { get; init; }
        public StorageType Type { get; init; }
    }

    public sealed class Archetypes : IDisposable
    {
        internal EntityMeta[] _meta = new EntityMeta[512];
        internal readonly Queue<Identity> _unusedIds = new();
        internal readonly List<Table> _tables = new();
        private readonly Dictionary<TSet, Table> _typeTableMap = new();
        // TODO: profile
        internal readonly Dictionary<TMask, Query> _queries = new();
        internal int _entityCount;
        internal readonly Dictionary<StorageType, List<Table>> _tablesByType = new();
        private static readonly StorageType s_entityType = StorageType.Create<Entity>();

        private readonly List<List<object>> _objectComponentsPool = new(512);
        // TODO: concurrent?
        internal Dictionary<Identity/*entity*/, Dictionary<StorageType/*component type*/, List<object>/*components*/>>
            EntityReferenceTypeComponents
        { get; } = new();

        public Archetypes()
        {
            var types = TSet.Create(s_entityType);
            AddTable(types, new TableStorage(types));
        }

        public Entity Spawn()
        {
            var identity = _unusedIds.Count > 0 ? _unusedIds.Dequeue() : new Identity(++_entityCount);
            var table = _tables[0];
            var row = table.Add(identity);
            if (_meta.Length == _entityCount) Array.Resize(ref _meta, _entityCount << 1);
            _meta[identity.Id] = new EntityMeta(identity, table.Id, row);
            var entity = new Entity(identity);
            EntityReferenceTypeComponents[identity] = new();
            table.GetStorage<Entity>()[row] = entity;
            return entity;
        }

        public void Despawn(Identity identity)
        {
            if (!IsAlive(identity)) return;

            var meta = _meta[identity.Id];
            var table = _tables[meta.TableId];
            table.Remove(meta.Row);
            _meta[identity.Id] = EntityMeta.Invalid;
            _unusedIds.Enqueue(identity);
            foreach (var objectComponents in EntityReferenceTypeComponents[identity].Values)
            {
                ReturnComponents(objectComponents);
            }
            EntityReferenceTypeComponents[identity].Clear();
        }

        public void BuildComponents<TBuilder>(Identity identity, TBuilder builder) where TBuilder : IComponentsBuilder
        {
            using var types = new PooledList<StorageType>(32);
            builder.CollectTypes(types.GetValue());
            if (types.Count == 0) return;
            AddComponentTypes(identity, types.GetValue());
            var referenceInstancesStorage = EntityReferenceTypeComponents[identity];
            foreach (var type in types)
            {
                if (!type.IsValueType) referenceInstancesStorage.TryAdd(type, RentComponents());
            }
            builder.Build(new ArchetypesBuilder(this), identity);
            WarningIfEmptyObject(identity, types);
        }

        public void AddComponent<T>(Identity identity, T data) where T : struct
        {
            var type = StorageType.Create<T>();
            var (table, row) = AddComponentType(identity, type);
            if (!type.IsTag)
            {
                var storage = table.GetStorage<T>();
                storage[row] = data;
            }
        }

        public void AddUntypedValueComponent(Identity identity, object data)
        {
            Debug.Assert(data.GetType().IsValueType);
            var type = StorageType.Create(data.GetType());
            var (table, row) = AddComponentType(identity, type);
            if (!type.IsTag)
            {
                var storage = table.GetStorage(type);
                storage.SetValue(data, row);
            }
        }

        public T AddObjectComponent<T>(Identity identity, T data) where T : class
        {
            if (data.GetType().IsValueType)
            {
                AddUntypedValueComponent(identity, data);
                return data;
            }

            var components = GetOrCreateComponentsStorage(identity, data);
            if (components.Count == 0) components.Add(data);
            else Debug.LogError($"there's existing type of {data.GetType()}, use `{nameof(AddMultipleObjectComponent)}` to add multiple component with same type onto the entity.", data as UnityEngine.Object);
            return data;
        }

        public T AddMultipleObjectComponent<T>(Identity identity, T data) where T : class
        {
            var storage = GetOrCreateComponentsStorage(identity, data);
            if (!storage.Contains(data)) storage.Add(data);
            return data;
        }

        private List<object> GetOrCreateComponentsStorage(Identity identity, object data)
        {
            WarningIfTagClass(data.GetType());
            if (data == null) throw new ArgumentNullException(nameof(data));
            var type = StorageType.Create(data.GetType());
            if (!EntityReferenceTypeComponents[identity].TryGetValue(type, out var components))
            {
                AddComponentType(identity, type);
                components = RentComponents();
                EntityReferenceTypeComponents[identity][type] = components;
            }
            return components;
        }

        public void RemoveObjectComponent<T>(Identity identity, T instance) where T : class
        {
            var type = StorageType.Create(instance.GetType());
            if (EntityReferenceTypeComponents[identity].TryGetValue(type, out var components))
            {
                components.Remove(instance);
                if (components.Count == 0) RemoveComponent(identity, instance.GetType());
            }
        }

        public void RemoveObjectComponent<T>(Identity identity) where T : class
        {
            RemoveComponentType(identity, StorageType.Create<T>());
            if (EntityReferenceTypeComponents[identity].Remove(StorageType.Create<T>(), out var components))
            {
                ReturnComponents(components);
            }
        }

        public void RemoveComponent<T>(Identity identity) where T : struct
        {
            RemoveComponentType(identity, StorageType.Create<T>());
        }

        public void RemoveComponent(Identity identity, Type type)
        {
            var storageType = StorageType.Create(type);
            RemoveComponentType(identity, storageType);
            if (EntityReferenceTypeComponents[identity].Remove(storageType, out var components))
            {
                ReturnComponents(components);
            }
        }

        private List<object> RentComponents()
        {
            if (_objectComponentsPool.Count > 0)
            {
                var index = _objectComponentsPool.Count - 1;
                var components = _objectComponentsPool[index];
                _objectComponentsPool.RemoveAt(index);
                return components;
            }
            return new List<object>(1);
        }

        private void ReturnComponents(List<object> components)
        {
            components.Clear();
            _objectComponentsPool.Add(components);
        }

        internal (Table table, int row) AddComponentTypes<TCollection>(Identity identity, TCollection types)
            where TCollection : IReadOnlyList<StorageType>
        {
            var meta = _meta[identity.Id];
            var oldTable = _tables[meta.TableId];

            using var newTypes = TSet.Create(oldTable.Types, Allocator.Temp);
            var hasNewValueType = false;
            for (var i = 0; i < types.Count; i++)
            {
                var type = types[i];
                WarningIfCanBeUnmanaged(type.Type);
                Debug.Assert(!oldTable.Types.Contains(type), $"Entity {identity} already has component of type {type.Type.Name}");
                newTypes.Add(type);
                hasNewValueType = hasNewValueType || type is { IsValueType: true, IsTag: false };
            }

            if (!_typeTableMap.TryGetValue(newTypes, out var newTable))
            {
                var persistentNewTypes = TSet.Create(newTypes);
                var storage = hasNewValueType ? new TableStorage(persistentNewTypes) : oldTable.TableStorage;
                newTable = AddTable(persistentNewTypes, storage);
            }

            var newRow = Table.MoveEntry(identity, meta.Row, oldTable, newTable);
            _meta[identity.Id] = new EntityMeta(identity, tableId: newTable.Id, row: newRow);
            return (newTable, newRow);
        }

        private (Table table, int row) AddComponentType(Identity identity, StorageType type)
        {
            return AddComponentTypes(identity, type.YieldStruct());
        }

        private void RemoveComponentType(Identity identity, StorageType type)
        {
            var meta = _meta[identity.Id];
            var oldTable = _tables[meta.TableId];

            if (!oldTable.Types.Contains(type))
            {
                throw new Exception($"cannot remove non-existent component {type.Type.Name} from entity {identity}");
            }

            var newTypes = TSet.Create(oldTable.Types);
            newTypes.Remove(type);

            if (!_typeTableMap.TryGetValue(newTypes, out var newTable))
            {
                var storage = type.IsValueType ? new TableStorage(newTypes) : oldTable.TableStorage;
                newTable = AddTable(newTypes, storage);
            }

            var newRow = Table.MoveEntry(identity, meta.Row, oldTable, newTable);
            _meta[identity.Id] = new EntityMeta(identity, row: newRow, tableId: newTable.Id);
        }

        public unsafe Span<byte> GetComponentRawData(Identity identity, StorageType type)
        {
            Debug.Assert(type.Type.IsUnmanaged());
            if (type.IsTag) return Span<byte>.Empty;

            var meta = _meta[identity.Id];
            var table = _tables[meta.TableId];
            var storage = table.GetStorage(type);
            Debug.Assert(storage.GetType().GetElementType() == type.Type);
            var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(storage, meta.Row);
            var size = Marshal.SizeOf(type.Type);
            return new Span<byte>(ptr.ToPointer(), size);
        }

        public void SetComponentRawData(Identity identity, StorageType type, Span<byte> data)
        {
            var component = GetComponentRawData(identity, type);
            Debug.Assert(data.Length == component.Length);
            data.CopyTo(component);
        }

        public ref T GetComponent<T>(Identity identity) where T : struct
        {
            Debug.Assert(!StorageType.Create<T>().IsTag);
            var meta = _meta[identity.Id];
            var table = _tables[meta.TableId];
            return ref table.GetStorage<T>()[meta.Row];
        }

        public bool HasComponent(StorageType type, Identity identity)
        {
            WarnSystemType(type.Type);
            var meta = _meta[identity.Id];
            return meta.Identity != Identity.None && _tables[meta.TableId].TypesInHierarchy.Contains(type);
        }

        public T GetObjectComponent<T>(Identity identity) where T : class
        {
            TryGetObjectComponent(identity, out T? component);
            Debug.Assert(component != null);
            return component!;
        }

        public bool TryGetObjectComponent<T>(Identity identity, out T? component) where T : class
        {
            var entityComponents = EntityReferenceTypeComponents[identity];
            var hasComponents = entityComponents.TryGetValue(StorageType.Create<T>(), out List<object>? value);
            if (hasComponents)
            {
                component = (T?)value![^1];
                return hasComponents;
            }
            // TODO: cache type hierarchy tree for optimization
            foreach (var (type, v) in entityComponents)
            {
                if (typeof(T).IsAssignableFrom(type.Type))
                {
                    component = (T)v[^1];
                    return true;
                }
            }
            component = null;
            return false;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT")]
        private static void WarnSystemType(Type type)
        {
            // HACK: system interfaces had been skipped
            if (!string.IsNullOrEmpty(type.Namespace) && type.Namespace.StartsWith("System.", StringComparison.InvariantCulture))
            {
                Game.Debug.LogWarning("don't use system interface as component of entity, they had been skipped for performance reason");
            }
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

        internal bool IsAlive(Identity identity)
        {
            return _meta[identity.Id].Identity != Identity.None;
        }

        internal EntityMeta GetEntityMeta(in Identity identity)
        {
            return _meta[identity.Id];
        }

        internal void SetEntityMeta(in Identity identity, in EntityMeta meta)
        {
            _meta[identity.Id] = meta;
        }

        internal Table GetTable(int tableId)
        {
            return _tables[tableId];
        }

        internal void GetAllValueComponents(Identity identity, ICollection<UntypedComponent> components)
        {
            var meta = _meta[identity.Id];
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
            foreach (var (key, value) in EntityReferenceTypeComponents[identity])
            {
                if (!typeof(T).IsAssignableFrom(key.Type)) continue;
                foreach (var obj in value) collection.Add((T)obj);
            }
        }

        private static readonly HashSet<Type> s_checkedTypes = new();

        [Conditional("DEBUG")]
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT")]
        private static void WarningIfCanBeUnmanaged(Type type)
        {
            if (s_checkedTypes.Contains(type)) return;
            s_checkedTypes.Add(type);
            if (!type.IsClass) return;
            if (type.BaseType != typeof(object)) return;
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fields.Any(fi => !fi.FieldType.IsUnmanaged())) return;
            Game.Debug.LogWarning($"{type} can be changed to `struct` for performance gain");
        }

        [Conditional("DEBUG")]
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT")]
        private static void WarningIfTagClass(Type type)
        {
            if (!type.IsValueType && StorageType.Create(type).IsTag)
                Game.Debug.LogWarning($"{type} can be changed to `struct` tag");
        }

        [Conditional("DEBUG")]
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT")]
        void WarningIfEmptyObject(Identity entity, List<StorageType> types)
        {
            foreach (var type in types)
            {
                if (EntityReferenceTypeComponents[entity].TryGetValue(type, out var objects))
                {
                    if (objects.Count == 0)
                        Debug.LogError($"Entity {entity} has empty component {type}");
                }
                else
                {
                    Debug.LogError($"Entity {entity} has no list of component {type}");
                }
            }
        }

        public void Dispose()
        {
            foreach (var table in _tables) table.Dispose();
            foreach (var query in _queries.Values) query.Mask.Dispose();
        }
    }
}
