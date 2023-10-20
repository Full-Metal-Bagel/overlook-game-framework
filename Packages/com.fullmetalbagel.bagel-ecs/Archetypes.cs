using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

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
        internal readonly Dictionary<int, Query> _queries = new();
        internal int _entityCount;
        internal readonly Dictionary<StorageType, List<Table>> _tablesByType = new();
        private static readonly StorageType s_entityType = StorageType.Create<Entity>();

        public Archetypes()
        {
            AddTable(new SortedSet<StorageType> { s_entityType });
        }

        public Entity Spawn()
        {
            var identity = _unusedIds.Count > 0 ? _unusedIds.Dequeue() : new Identity(++_entityCount);
            var table = _tables[0];
            var row = table.Add(identity);
            if (_meta.Length == _entityCount) Array.Resize(ref _meta, _entityCount << 1);
            _meta[identity.Id] = new EntityMeta(identity, table.Id, row);
            var entity = new Entity(identity);
            table.GetStorage<Entity>()[row] = entity;
            return entity;
        }

        public void Despawn(Identity identity)
        {
            if (!IsAlive(identity)) return;

            var meta = _meta[identity.Id];
            var table = _tables[meta.TableId];
            table.Remove(meta.Row);
            _meta[identity.Id] = new EntityMeta(Identity.None, row: 0, tableId: 0);
            _unusedIds.Enqueue(identity);
        }

        public ref T AddComponent<T>(Identity identity, T data) where T : struct
        {
            var type = StorageType.Create<T>();
            var (table, row) = AddComponent(identity, type);
            var storage = table.GetStorage<T>();
            storage[row] = data;
            return ref storage[row];
        }

        public void AddObjectComponent<T>(Identity identity, T data) where T : class
        {
            var type = StorageType.Create(data.GetType());
            var (table, row) = AddComponent(identity, type);
            table.GetStorage(type).SetValue(data, row);
        }

        private (Table table, int row) AddComponent(Identity identity, StorageType type)
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
                var newTypes = new SortedSet<StorageType>(oldTable.Types);
                newTypes.Add(type);
                newTable = AddTable(newTypes);
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

        public ref T GetComponent<T>(Identity identity) where T : struct
        {
            var meta = _meta[identity.Id];
            var table = _tables[meta.TableId];
            return ref table.GetStorage<T>()[meta.Row];
        }

        public T GetObjectComponent<T>(Identity identity) where T : class
        {
            var meta = _meta[identity.Id];
            var table = _tables[meta.TableId];
            return (T)table.GetStorage(StorageType.Create<T>()).GetValue(meta.Row);
        }

        public bool HasComponent(StorageType type, Identity identity)
        {
            var meta = _meta[identity.Id];
            return meta.Identity != Identity.None && _tables[meta.TableId].TypesInHierarchy.Contains(type);
        }

        public void RemoveComponent(StorageType type, Identity identity)
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
                var newTypes = new SortedSet<StorageType>(oldTable.Types);
                newTypes.Remove(type);
                newTable = AddTable(newTypes);
                oldEdge.Remove = newTable;

                var newEdge = newTable.GetTableEdge(type);
                newEdge.Add = oldTable;

                _tables.Add(newTable);
            }

            var newRow = Table.MoveEntry(identity, meta.Row, oldTable, newTable);
            _meta[identity.Id] = new EntityMeta(identity, row: newRow, tableId: newTable.Id);
        }

        internal Query GetQuery(Mask mask, Func<Archetypes, Mask, List<Table>, Query> createQuery)
        {
            // TODO: replace hash by something more safer? Set? BitArray?
            var hash = mask.GetHashCode();

            if (_queries.TryGetValue(hash, out var query))
            {
                MaskPool.Add(mask);
                return query;
            }

            var matchingTables = new List<Table>();

            var type = mask._hasTypes.Count == 0 ? StorageType.Create<Entity>() : mask._hasTypes[0];
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
            _queries.Add(hash, query);

            return query;
        }

        internal static bool IsMaskCompatibleWith(Mask mask, Table table)
        {
            var matchesComponents = table.TypesInHierarchy.IsSupersetOf(mask._hasTypes);
            matchesComponents = matchesComponents && !table.TypesInHierarchy.Overlaps(mask._notTypes);
            matchesComponents = matchesComponents && (mask._anyTypes.Count == 0 || table.TypesInHierarchy.Overlaps(mask._anyTypes));
            return matchesComponents;
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

        internal void FindAllComponents(Identity identity, ICollection<UntypedComponent> components)
        {
            var meta = _meta[identity.Id];
            var table = _tables[meta.TableId];
            foreach (var (type, storage) in table)
            {
                components.Add(new UntypedComponent { Storage = storage, Row = meta.Row, Type = type });
            }
        }

        internal void FindComponents<T>(Identity identity, Type realType, ICollection<T> components) where T : class
        {
            var meta = _meta[identity.Id];
            var table = _tables[meta.TableId];

            foreach (var (type, storage) in table)
            {
                if (realType.IsAssignableFrom(type.Type))
                    components.Add(((T[])storage)[meta.Row]);
            }
        }

        Table AddTable(SortedSet<StorageType> types)
        {
            var table = new Table(_tables.Count, this, types);
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
