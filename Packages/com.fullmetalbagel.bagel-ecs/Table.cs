using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

        public readonly SortedSet<StorageType> Types;

        public Identity[] Identities => _identities;

        public int Count { get; private set; }
        public bool IsEmpty => Count == 0;

        readonly Archetypes _archetypes;

        Identity[] _identities;

        readonly Dictionary<StorageType, TableEdge> _edges = new();
        readonly Dictionary<StorageType, Array> _storages = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Table(int id, Archetypes archetypes, SortedSet<StorageType> types)
        {
            _archetypes = archetypes;

            Id = id;
            Types = types;

            _identities = new Identity[StartCapacity];

            foreach (var type in types)
            {
                _storages[type] = Array.CreateInstance(type.Type, StartCapacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Add(Identity identity)
        {
            EnsureCapacity(Count + 1);
            _identities[Count] = identity;
            return Count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TableEdge GetTableEdge(StorageType type)
        {
            if (_edges.TryGetValue(type, out var edge)) return edge;

            edge = new TableEdge();
            _edges[type] = edge;

            return edge;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] GetStorage<T>(Identity target)
        {
            var type = StorageType.Create<T>(target);
            return (T[])GetStorage(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Array GetStorage(StorageType type)
        {
            return _storages[type];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EnsureCapacity(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "minCapacity must be positive");
            if (capacity <= _identities.Length) return;

            Resize(Math.Max(capacity, StartCapacity) << 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
