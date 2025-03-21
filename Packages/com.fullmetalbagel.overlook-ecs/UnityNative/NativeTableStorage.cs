#nullable enable

#if ARCHETYPE_USE_UNITY_NATIVE_COLLECTION

using System;
using System.Diagnostics;
using AOT;
using Game;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Debug = Game.Debug;

namespace RelEcs
{
    public static class NativeTableStorage
    {
        public delegate int ExpandCapacity(int currentCapacity, int expectCapacity, int entitySize);

        [BurstCompile]
        [MonoPInvokeCallback(typeof(ExpandCapacity))]
        public static int DefaultExpandFunc(int currentCapacity, int expectCapacity, int entitySize)
        {
            int minimumExpandingCapacity = 32;
            int minimumExpandingBytes = 1024;
            var expanding = math.max(minimumExpandingCapacity, minimumExpandingBytes / entitySize);
            return math.max(currentCapacity + expanding, expectCapacity);
        }
    }

    // TODO: avoid modifying `Capacity` and `_storage` directly
    //       treat `NativeTableStorage` as immutable collection for multi-threaded jobs
    [DisallowDefaultConstructor]
    public unsafe struct NativeTableStorage<T> : INativeDisposable where T : unmanaged, IFixedSize
    {
        public int Id { get; }
        public int Capacity { get; private set; }
        public static int AlignmentSize => sizeof(T);

        public FunctionPointer<NativeTableStorage.ExpandCapacity> ExpandCapacityFunc { get; init; }
            = BurstCompiler.CompileFunctionPointer<NativeTableStorage.ExpandCapacity>(NativeTableStorage.DefaultExpandFunc);

        static NativeTableStorage()
        {
            Debug.Assert((AlignmentSize & (AlignmentSize - 1)) == 0, $"Type {typeof(T)} size must be power of 2");
        }

        // storage
        private NativeHashMap<StorageType, int/*index*/> _indices;
        private NativeArray<int> _typeSizeOffsets;
        private NativeArray<int> _typeStorageOffsets;
        private NativeArray<T> _storage;

        // TODO: sort unused indices by descending order?
        private NativeList<int> UnusedList { get; }

        private readonly Allocator _allocator;

        public NativeTableStorage(int id, NativeBitArraySet types, Allocator allocator) : this()
        {
            Id = id;
            _allocator = allocator;
            int entitySize = 0;
            using var validTypes = new NativeList<StorageType>(types.Length, Allocator.Temp);
            foreach (var type in types)
            {
                if (type is { IsUnmanagedType: true, IsTag: false })
                {
                    validTypes.Add(type);
                    entitySize += type.UnmanagedTypeSize;
                }
            }

            Capacity = ExpandCapacityFunc.Invoke(Capacity, 1, entitySize);

            _indices = new NativeHashMap<StorageType, int>(validTypes.Length, allocator);
            _typeSizeOffsets = new NativeArray<int>(validTypes.Length + 1, allocator, NativeArrayOptions.UninitializedMemory);
            _typeStorageOffsets = new NativeArray<int>(validTypes.Length + 1, allocator, NativeArrayOptions.UninitializedMemory);
            _typeSizeOffsets[0] = 0;
            _typeStorageOffsets[0] = 0;
            for (int i = 0; i < validTypes.Length; i++)
            {
                StorageType type = validTypes[i];
                _indices[type] = i;
                _typeSizeOffsets[i + 1] = _typeSizeOffsets[i] + type.UnmanagedTypeSize;
                var storageSize = GetAlignedSize(type.UnmanagedTypeSize * Capacity);
                _typeStorageOffsets[i + 1] = _typeStorageOffsets[i] + storageSize;
            }

            _storage = new NativeArray<T>(_typeStorageOffsets[^1], allocator, NativeArrayOptions.UninitializedMemory);
            UnusedList = new NativeList<int>(Capacity, allocator);
            for (var i = 0; i < Capacity; i++) UnusedList.Add(i);
        }

        [Pure, MustUseReturnValue]
        public static int GetAlignedSize(int size)
        {
            return (size + AlignmentSize - 1) / AlignmentSize;
        }

        public NativeSlice<byte> GetStorage(StorageType type)
        {
            Debug.Assert(type is { IsUnmanagedType: true, IsTag: false });
            var index = _indices[type];
            var start = _typeStorageOffsets[index];
            var end = _typeStorageOffsets[index + 1];
            var length = type.UnmanagedTypeSize * Capacity;
            Debug.Assert(length <= (end - start) * sizeof(T));
            return _storage.Slice(start, end - start).SliceConvert<byte>().Slice(0, length);
        }

        public int RentRow()
        {
            if (UnusedList.Length == 0) Expand();
            var lastIndex = UnusedList.Length - 1;
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
            capacity = ExpandCapacityFunc.Invoke(currentCapacity: Capacity, expectCapacity: capacity, entitySize: _typeSizeOffsets[^1]);
            Expand(capacity);
        }

        private void Expand()
        {
            EnsureCapacity(Capacity + 1);
        }

        private void Expand(int newCapacity)
        {
            Debug.Assert(newCapacity > Capacity);
            for (var index = Capacity; index < newCapacity; index++)
                UnusedList.Add(index);

            for (int i = 1; i < _typeSizeOffsets.Length; i++)
            {
                var typeSize = _typeSizeOffsets[i] - _typeSizeOffsets[i - 1];
                var storageSize = GetAlignedSize(typeSize * newCapacity);
                _typeStorageOffsets[i] = _typeStorageOffsets[i - 1] + storageSize;
            }

            WarningIfExceed64KB(_typeStorageOffsets[^1] * sizeof(T));
            var newStorage = new NativeArray<T>(_typeStorageOffsets[^1], _allocator);
            var oldStorageIndex = 0;
            for (int i = 1; i < _typeSizeOffsets.Length; i++)
            {
                var typeSize = _typeSizeOffsets[i] - _typeSizeOffsets[i - 1];
                var oldStorageSize = GetAlignedSize(typeSize * Capacity);
                var old = _storage.Slice(oldStorageIndex, oldStorageSize);
                oldStorageIndex += oldStorageSize;
                var @new = newStorage.Slice(_typeStorageOffsets[i - 1], _typeStorageOffsets[i] - _typeStorageOffsets[i - 1]);
                @new.Slice(0, old.Length).CopyFrom(old);
            }
            _storage.Dispose();
            _storage = newStorage;

            Capacity = newCapacity;
        }

        [Conditional("KGP_DEBUG")]
        private static void WarningIfExceed64KB(int length)
        {
            if (length > 1024 * 64) Debug.LogWarning($"Storage size ({length / 1024f:0.00}KB) is larger than 64KB");
        }

        public void Dispose()
        {
            _storage.Dispose();
            _indices.Dispose();
            _typeSizeOffsets.Dispose();
            _typeStorageOffsets.Dispose();
            UnusedList.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            var handlers = new NativeArray<JobHandle>(5, Allocator.TempJob);
            handlers[0] = _storage.Dispose(inputDeps);
            handlers[1] = _indices.Dispose(inputDeps);
            handlers[2] = _typeSizeOffsets.Dispose(inputDeps);
            handlers[3] = _typeStorageOffsets.Dispose(inputDeps);
            handlers[4] = UnusedList.Dispose(inputDeps);
            return handlers.Dispose(JobHandle.CombineDependencies(handlers));
        }
    }
}

#endif
