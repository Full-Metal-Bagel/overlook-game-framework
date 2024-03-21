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
    public sealed class TableStorage
    {
        private const int InitCapacity = 4;
        private const int MinimumExpandingCapacity = 32;

        private readonly Dictionary<StorageType, Array> _storages = new();
        public IReadOnlyDictionary<StorageType, Array> Storages => _storages;

        public int Capacity { get; private set; } = InitCapacity;

        public delegate int ExpandCapacity(int current, int expect);
        public ExpandCapacity ExpandCapacityFunc { get; init; } = (current, expect) => Math.Max(current + MinimumExpandingCapacity, expect);

        // TODO: sort unused indices by descending order?
        private List<int> UnusedList { get; } = new(32);

        public TableStorage(TSet types)
        {
            foreach (var type in types)
            {
                if (type.IsValueType)
                    _storages[type] = Array.CreateInstance(type.Type, Capacity);
            }

            for (var index = 0; index < Capacity; index++)
                UnusedList.Add(index);
        }

        public Array GetStorage(StorageType type)
        {
            Debug.Assert(type.IsValueType);
            if (Storages.TryGetValue(type, out var array)) return array;
            throw new ArgumentException($"invalid StorageType: {type.Type}");
        }

        public int RentRow()
        {
            if (UnusedList.Count == 0) Expand();
            var lastIndex = UnusedList.Count - 1;
            var row = UnusedList[lastIndex];
            UnusedList.RemoveAt(lastIndex);
            return row;
        }

        public void ReleaseRow(int row)
        {
            Debug.Assert(!UnusedList.Contains(row));
            if (row >= Capacity)
                throw new ArgumentOutOfRangeException(nameof(row), "row cannot be greater or equal to capacity");
            UnusedList.Add(row);
        }

        internal void EnsureCapacity(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "minCapacity must be positive");
            if (capacity <= Capacity) return;
            capacity = ExpandCapacityFunc(current: Capacity, expect: capacity);
            Expand(capacity);
        }

        private void Expand()
        {
            EnsureCapacity(Capacity + 1);
        }

        private void Expand(int capacity)
        {
            Debug.Assert(capacity > Capacity);
            for (var index = Capacity; index < capacity; index++)
                UnusedList.Add(index);
            Capacity = capacity;

            // NOTE: 512 just a random-picked-reasonable-enough number
            Span<StorageType> keys = _storages.Count <= 512 ?
                stackalloc StorageType[_storages.Count] :
                new StorageType[_storages.Count]
            ;
            int i = 0;
            foreach (var key in _storages.Keys)
            {
                keys[i] = key;
                i++;
            }
            foreach (var type in keys)
            {
                var storage = _storages[type];
                var elementType = storage.GetType().GetElementType()!;
                var newStorage = Array.CreateInstance(elementType, capacity);
                Array.Copy(storage, newStorage, Math.Min(storage.Length, capacity));
                _storages[type] = newStorage;
            }
        }
    }
}
