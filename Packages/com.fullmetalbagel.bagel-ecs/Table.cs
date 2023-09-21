using System;
using System.Collections.Generic;

namespace RelEcs
{
    public sealed class TableEdge
    {
        public Table? Add;
        public Table? Remove;
    }

    public sealed class Table
    {
        const int StartCapacity = 4;

        public readonly int Id;

        public SortedSet<StorageType> Types { get; }
        public SortedSet<StorageType> TypesInHierarchy { get; } = new();

        public Identity[] Identities => _identities;

        public int Count { get; private set; }
        public bool IsEmpty => Count == 0;

        readonly Archetypes _archetypes;

        Identity[] _identities;

        readonly Dictionary<StorageType, TableEdge> _edges = new();
        readonly Dictionary<StorageType, Array> _storages = new();

        public IReadOnlyDictionary<StorageType, Array> Storages => _storages;

        public Table(int id, Archetypes archetypes, SortedSet<StorageType> types)
        {
            _archetypes = archetypes;

            Id = id;
            Types = types;

            _identities = new Identity[StartCapacity];

            foreach (var type in types)
            {
                FillAllTypes(type, TypesInHierarchy);
                _storages[type] = Array.CreateInstance(type.Type, StartCapacity);
            }

            // TODO: cache
            static void FillAllTypes(StorageType storageType, SortedSet<StorageType> set)
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
            EnsureCapacity(Count + 1);
            _identities[Count] = identity;
            return Count++;
        }

        public void Remove(int row)
        {
            if (row >= Count)
                throw new ArgumentOutOfRangeException(nameof(row), "row cannot be greater or equal to count");

            Count--;

            if (row < Count)
            {
                _identities[row] = _identities[Count];

                foreach (var storage in _storages.Values)
                {
                    Array.Copy(storage, Count, storage, row, 1);
                }

                _archetypes.GetEntityMeta(_identities[row]).Row = row;
            }

            _identities[Count] = Identity.None;

            foreach (var storage in _storages.Values)
            {
                Array.Clear(storage, Count, 1);
            }
        }

        public TableEdge GetTableEdge(StorageType type)
        {
            if (_edges.TryGetValue(type, out var edge)) return edge;

            edge = new TableEdge();
            _edges[type] = edge;

            return edge;
        }

        public T[] GetStorage<T>()
        {
            var type = StorageType.Create<T>();
            return (T[])GetStorage(type);
        }

        public Array GetStorage(StorageType type)
        {
            if (_storages.TryGetValue(type, out var array)) return array;
            // TODO: optimize by building map of base/interface -> actualType during creation
            foreach (var (storageType, storage) in Storages)
            {
                if (type.Type.IsAssignableFrom(storageType.Type))
                    return storage;
            }
            throw new ArgumentException($"invalid StorageType: {type}");
        }

        internal void EnsureCapacity(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "minCapacity must be positive");
            if (capacity <= _identities.Length) return;

            Resize(Math.Max(capacity, StartCapacity) << 1);
        }

        void Resize(int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "length cannot be negative");
            if (length < Count)
                throw new ArgumentOutOfRangeException(nameof(length), "length cannot be smaller than Count");

            Array.Resize(ref _identities, length);

            foreach (var (type, storage) in _storages)
            {
                var elementType = storage.GetType().GetElementType()!;
                var newStorage = Array.CreateInstance(elementType, length);
                Array.Copy(storage, newStorage, Math.Min(storage.Length, length));
                _storages[type] = newStorage;
            }
        }

        public static int MoveEntry(Identity identity, int oldRow, Table oldTable, Table newTable)
        {
            var newRow = newTable.Add(identity);

            foreach (var (type, oldStorage) in oldTable._storages)
            {
                if (!newTable._storages.TryGetValue(type, out var newStorage)) continue;

                Array.Copy(oldStorage, oldRow, newStorage, newRow, 1);
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
    }
}
