using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

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

    public sealed class Archetypes
    {
        internal EntityMeta[] _meta = new EntityMeta[512];
        internal readonly Queue<Identity> _unusedIds = new();
        internal readonly List<Table> _tables = new();
        // TODO: profile
        internal readonly Dictionary<TMask, Query> _queries = new();
        internal int _entityCount;
        internal readonly Dictionary<StorageType, List<Table>> _tablesByType = new();
        private static readonly StorageType s_entityType = StorageType.Create<Entity>();

        // TODO: concurrent?
        internal Dictionary<Identity/*entity*/, Dictionary<StorageType/*component type*/, object/*components*/>>
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
            EntityReferenceTypeComponents[identity].Clear();
        }

        public ref T AddComponent<T>(Identity identity, T data) where T : struct
        {
            var type = StorageType.Create<T>();
            var (table, row) = AddComponent(identity, type);
            var storage = table.GetStorage<T>();
            storage[row] = data;
            return ref storage[row];
        }

        public void AddUntypedValueComponent(Identity identity, object data)
        {
            Debug.Assert(data.GetType().IsValueType);
            var type = StorageType.Create(data.GetType());
            var (table, row) = AddComponent(identity, type);
            var storage = table.GetStorage(type);
            storage.SetValue(data, row);
        }

        public T AddObjectComponent<T>(Identity identity, T data) where T : class
        {
            Debug.Assert(!data.GetType().IsValueType);
            if (data == null) throw new ArgumentNullException(nameof(data));
            var type = StorageType.Create(data.GetType());
            AddComponent(identity, type);
            EntityReferenceTypeComponents[identity][type] = data;
            return data;
        }

        internal (Table table, int row) AddComponent(Identity identity, StorageType type)
        {
            WarningIfCanBeUnmanaged(type.Type);

            var meta = _meta[identity.Id];
            var oldTable = _tables[meta.TableId];

            if (oldTable.Types.Contains(type))
            {
                throw new Exception($"Entity {identity} already has component of type {type.Type.Name}");
            }

            var oldEdge = oldTable.GetTableEdge(type);
            var newTable = oldEdge.Add;
            if (newTable == null)
            {
                var newTypes = TSet.Create(oldTable.Types);
                newTypes.Add(type);
                var storage = type.IsValueType ? new TableStorage(newTypes) : oldTable.TableStorage;
                newTable = AddTable(newTypes, storage);
                oldEdge.Add = newTable;

                var newEdge = newTable.GetTableEdge(type);
                newEdge.Remove = oldTable;
            }

            var newRow = Table.MoveEntry(identity, meta.Row, oldTable, newTable);
            _meta[identity.Id] = new EntityMeta(identity, tableId: newTable.Id, row: newRow);
            return (newTable, newRow);
        }

        public unsafe Span<byte> GetComponentRawData(Identity identity, StorageType type)
        {
            Debug.Assert(type.Type.IsUnmanaged());
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
            return component;
        }

        public bool TryGetObjectComponent<T>(Identity identity, out T? component) where T : class
        {
            var entityComponents = EntityReferenceTypeComponents[identity];
            var hasComponent = entityComponents.TryGetValue(StorageType.Create<T>(), out var value);
            if (hasComponent)
            {
                component = (T?)value;
                return hasComponent;
            }
            // TODO: cache type hierarchy tree for optimization
            foreach (var (type, v) in entityComponents)
            {
                if (typeof(T).IsAssignableFrom(type.Type))
                {
                    component = (T)v;
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

        public void RemoveComponent(Identity identity, StorageType type)
        {
            var meta = _meta[identity.Id];
            var oldTable = _tables[meta.TableId];

            if (!oldTable.Types.Contains(type))
            {
                throw new Exception($"cannot remove non-existent component {type.Type.Name} from entity {identity}");
            }

            var oldEdge = oldTable.GetTableEdge(type);

            var newTable = oldEdge.Remove;

            if (newTable == null)
            {
                var newTypes = TSet.Create(oldTable.Types);
                newTypes.Remove(type);
                var storage = type.IsValueType ? new TableStorage(newTypes) : oldTable.TableStorage;
                newTable = AddTable(newTypes, storage);
                oldEdge.Remove = newTable;

                var newEdge = newTable.GetTableEdge(type);
                newEdge.Add = oldTable;

                _tables.Add(newTable);
            }

            var newRow = Table.MoveEntry(identity, meta.Row, oldTable, newTable);
            _meta[identity.Id] = new EntityMeta(identity, row: newRow, tableId: newTable.Id);
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

        Table AddTable(TSet types, TableStorage storage)
        {
            var table = new Table(_tables.Count, types, storage);
            _tables.Add(table);

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
                if (typeof(T).IsAssignableFrom(key.Type))
                    collection.Add((T)value);
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
    }
}
