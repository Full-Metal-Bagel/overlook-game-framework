using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace RelEcs
{
    public sealed class Archetypes
    {
        internal EntityMeta[] Meta = new EntityMeta[512];

        internal readonly Queue<Identity> UnusedIds = new();

        internal readonly List<Table> Tables = new();

        internal readonly Dictionary<int, Query> Queries = new();

        internal int EntityCount;

        readonly List<TableOperation> _tableOperations = new();
        internal readonly Dictionary<StorageType, List<Table>> TablesByType = new();

        int _lockCount;
        bool _isLocked;

        private static readonly StorageType s_entityType = StorageType.Create<Entity>();

        public Archetypes()
        {
            AddTable(new SortedSet<StorageType> { s_entityType });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity Spawn()
        {
            var identity = UnusedIds.Count > 0 ? UnusedIds.Dequeue() : new Identity(++EntityCount);

            var table = Tables[0];

            var row = table.Add(identity);

            if (Meta.Length == EntityCount) Array.Resize(ref Meta, EntityCount << 1);

            Meta[identity.Id] = new EntityMeta(identity, table.Id, row);

            var entity = new Entity(identity);

            table.GetStorage(s_entityType).SetValue(entity, row);

            return entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Despawn(Identity identity)
        {
            if (!IsAlive(identity)) return;

            if (_isLocked)
            {
                _tableOperations.Add(new TableOperation { Despawn = true, Identity = identity });
                return;
            }

            ref var meta = ref Meta[identity.Id];

            var table = Tables[meta.TableId];

            table.Remove(meta.Row);

            meta.Row = 0;
            meta.Identity = Identity.None;

            UnusedIds.Enqueue(identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent(StorageType type, Identity identity, object data)
        {
            ref var meta = ref Meta[identity.Id];
            var oldTable = Tables[meta.TableId];

            if (oldTable.Types.Contains(type))
            {
                throw new Exception($"Entity {identity} already has component of type {type}");
            }

            if (_isLocked)
            {
                _tableOperations.Add(new TableOperation { Add = true, Identity = identity, Type = type, Data = data });
                return;
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

            meta.Row = newRow;
            meta.TableId = newTable.Id;

            var storage = newTable.GetStorage(type);
            storage.SetValue(data, newRow);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetComponent(StorageType type, Identity identity)
        {
            var meta = Meta[identity.Id];
            var table = Tables[meta.TableId];
            var storage = table.GetStorage(type);
            return storage.GetValue(meta.Row);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(StorageType type, Identity identity)
        {
            var meta = Meta[identity.Id];
            return meta.Identity != Identity.None && Tables[meta.TableId].TypesInHierarchy.Contains(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent(StorageType type, Identity identity)
        {
            ref var meta = ref Meta[identity.Id];
            var oldTable = Tables[meta.TableId];

            if (!oldTable.Types.Contains(type))
            {
                throw new Exception($"cannot remove non-existent component {type.Type.Name} from entity {identity}");
            }

            if (_isLocked)
            {
                _tableOperations.Add(new TableOperation { Add = false, Identity = identity, Type = type });
                return;
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

                Tables.Add(newTable);
            }

            var newRow = Table.MoveEntry(identity, meta.Row, oldTable, newTable);

            meta.Row = newRow;
            meta.TableId = newTable.Id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Query GetQuery(Mask mask, Func<Archetypes, Mask, List<Table>, Query> createQuery)
        {
            // TODO: replace hash by something more safer? Set? BitArray?
            var hash = mask.GetHashCode();

            if (Queries.TryGetValue(hash, out var query))
            {
                MaskPool.Add(mask);
                return query;
            }

            var matchingTables = new List<Table>();

            var type = mask.HasTypes[0];
            if (!TablesByType.TryGetValue(type, out var typeTables))
            {
                typeTables = new List<Table>();
                TablesByType[type] = typeTables;
            }

            foreach (var table in typeTables)
            {
                if (!IsMaskCompatibleWith(mask, table)) continue;

                matchingTables.Add(table);
            }

            query = createQuery(this, mask, matchingTables);
            Queries.Add(hash, query);

            return query;
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsMaskCompatibleWith(Mask mask, Table table)
        {
            var matchesComponents = table.TypesInHierarchy.IsSupersetOf(mask.HasTypes);
            matchesComponents = matchesComponents && !table.TypesInHierarchy.Overlaps(mask.NotTypes);
            matchesComponents = matchesComponents && (mask.AnyTypes.Count == 0 || table.TypesInHierarchy.Overlaps(mask.AnyTypes));
            return matchesComponents;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsAlive(Identity identity)
        {
            return Meta[identity.Id].Identity != Identity.None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref EntityMeta GetEntityMeta(Identity identity)
        {
            return ref Meta[identity.Id];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Table GetTable(int tableId)
        {
            return Tables[tableId];
        }

        internal void GetComponents<T>(Identity identity, ICollection<T> components)
        {
            var meta = Meta[identity.Id];
            var table = Tables[meta.TableId];

            foreach (var (type, storage) in table.Storages)
            {
                if (typeof(T).IsAssignableFrom(type.Type))
                    components.Add((T)storage.GetValue(meta.Row));
            }
        }

        internal void GetComponents(Identity identity, ICollection<object> components)
        {
            var meta = Meta[identity.Id];
            var table = Tables[meta.TableId];

            foreach (var storage in table.Storages.Values)
            {
                components.Add(storage.GetValue(meta.Row));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal (StorageType, object?)[] GetComponents(Identity identity)
        {
            var meta = Meta[identity.Id];
            var table = Tables[meta.TableId];

            var list = ListPool<(StorageType, object?)>.Get();

            foreach (var type in table.Types)
            {
                var storage = table.GetStorage(type);
                list.Add((type, storage.GetValue(meta.Row)));
            }

            var array = list.ToArray();
            ListPool<(StorageType, object?)>.Add(list);
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Table AddTable(SortedSet<StorageType> types)
        {
            var table = new Table(Tables.Count, this, types);
            Tables.Add(table);

            foreach (var type in table.TypesInHierarchy)
            {
                if (!TablesByType.TryGetValue(type, out var tableList))
                {
                    tableList = new List<Table>();
                    TablesByType[type] = tableList;
                }

                tableList.Add(table);
            }

            foreach (var query in Queries.Values.Where(query => IsMaskCompatibleWith(query.Mask, table)))
            {
                query.AddTable(table);
            }

            return table;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ApplyTableOperations()
        {
            foreach (var op in _tableOperations)
            {
                if (!IsAlive(op.Identity)) continue;

                if (op.Despawn) Despawn(op.Identity);
                else if (op.Add) AddComponent(op.Type, op.Identity, op.Data);
                else RemoveComponent(op.Type, op.Identity);
            }

            _tableOperations.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Lock()
        {
            _lockCount++;
            _isLocked = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unlock()
        {
            _lockCount--;
            if (_lockCount != 0) return;
            _isLocked = false;

            ApplyTableOperations();
        }

        struct TableOperation
        {
            public bool Despawn;
            public bool Add;
            public StorageType Type;
            public Identity Identity;
            public object Data;
        }
    }
}
