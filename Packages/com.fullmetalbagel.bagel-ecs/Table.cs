using System;
using System.Collections.Generic;
using Game;
#if ARCHETYPE_USE_NATIVE_BIT_ARRAY
using TSet = RelEcs.NativeBitArraySet;
#else
using TSet = RelEcs.SortedSetTypeSet;
#endif

namespace RelEcs
{
    public sealed class TableEdge
    {
        public Table? Add { get; set; }
        public Table? Remove { get; set; }
    }

    public sealed class Table : IDisposable
    {
        public int Id { get; }

        public TSet Types { get; }
        public TSet TypesInHierarchy { get; } = TSet.Create();

        public IList<Identity> Identities => _sortedIdentities.Values;
        public IList<int> Rows => _sortedIdentities.Keys;

        public int Count => _sortedIdentities.Count;
        public bool IsEmpty => Count == 0;

        internal TableStorage TableStorage { get; }
        private readonly SortedList<int /*row*/, Identity> _sortedIdentities = new(32);
        private readonly Dictionary<StorageType, TableEdge> _edges = new();

        public Table(int id, TSet types, TableStorage tableStorage)
        {
            TableStorage = tableStorage;

            Id = id;
            Types = types;

            foreach (var type in types)
            {
                FillAllTypes(type, TypesInHierarchy);
            }

            // TODO: cache
            static void FillAllTypes(StorageType storageType, TSet set)
            {
                var type = storageType.Type;
                foreach (var interfaceType in type.GetInterfaces())
                {
                    // HACK: skip system interfaces
                    if (!interfaceType.Namespace!.StartsWith("System.", StringComparison.InvariantCultureIgnoreCase))
                        set.Add(StorageType.Create(interfaceType));
                }

                while (type != null)
                {
                    set.Add(StorageType.Create(type));
                    type = type.BaseType;
                }
            }
        }

        public int Add(Identity identity)
        {
            var row = TableStorage.RentRow();
            _sortedIdentities.Add(row, identity);
            return row;
        }

        public void Remove(int row)
        {
            Debug.Assert(_sortedIdentities.ContainsKey(row));
            _sortedIdentities.Remove(row);
            TableStorage.ReleaseRow(row);
        }

        public TableEdge GetTableEdge(StorageType type)
        {
            if (_edges.TryGetValue(type, out var edge)) return edge;

            edge = new TableEdge();
            _edges[type] = edge;

            return edge;
        }

        internal T[] GetStorage<T>() where T : struct
        {
            return (T[])GetStorage(StorageType.Create<T>());
        }

        internal Array GetStorage(StorageType type)
        {
            return TableStorage.GetStorage(type);
        }

        public static int MoveEntry(Identity identity, int oldRow, Table oldTable, Table newTable)
        {
            if (ReferenceEquals(oldTable.TableStorage, newTable.TableStorage))
            {
                Debug.Assert(oldTable._sortedIdentities.ContainsKey(oldRow));
                Debug.Assert(!newTable._sortedIdentities.ContainsKey(oldRow));
                oldTable._sortedIdentities.Remove(oldRow);
                newTable._sortedIdentities.Add(oldRow, identity);
                return oldRow;
            }

            var newRow = newTable.Add(identity);
            foreach (var (type, oldStorage) in oldTable.TableStorage.Storages)
            {
                if (type.IsValueType && newTable.TableStorage.Storages.TryGetValue(type, out var newStorage))
                {
                    Array.Copy(oldStorage, oldRow, newStorage, newRow, 1);
                }
            }
            oldTable.Remove(oldRow);
            return newRow;
        }

        public override string ToString()
        {
            var s = $"Table {Id} ";
            foreach (var type in Types)
            {
                s += $"{type} ";
            }
            return s;
        }

        public void Dispose()
        {
            Types.Dispose();
            TypesInHierarchy.Dispose();
        }
    }
}
