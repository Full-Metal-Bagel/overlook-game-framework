#if ARCHETYPE_USE_UNITY_NATIVE_COLLECTION

using System;
using AOT;
using NUnit.Framework;
using Unity.Collections;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;
using TTableStorage = RelEcs.NativeTableStorage<RelEcs.Fixed8Bytes>;

namespace RelEcs.Tests
{
    public class NativeTableStorageTests
    {
        private NativeBitArraySet _types;
        private TTableStorage _storage;

        [SetUp]
        public void Setup()
        {
            _types = NativeBitArraySet.Create();
            // Add some test types
            _types.Add(StorageType.Create<int>());
            _types.Add(StorageType.Create<float>());
            _types.Add(StorageType.Create<Vector3>());
            _storage = new TTableStorage(0, _types, Allocator.Persistent);
        }

        [TearDown]
        public void TearDown()
        {
            _storage.Dispose();
            _types.Dispose();
        }

        [Test]
        public void InitialCapacity_ShouldNotBeZero()
        {
            Assert.That(_storage.Capacity, Is.Not.Zero);
        }

        [Test]
        public void RentRow_ShouldReturnUniqueRows()
        {
            var row1 = _storage.RentRow();
            var row2 = _storage.RentRow();
            var row3 = _storage.RentRow();

            Assert.That(row1, Is.Not.EqualTo(row2));
            Assert.That(row2, Is.Not.EqualTo(row3));
            Assert.That(row3, Is.Not.EqualTo(row1));
        }

        [Test]
        public void RentRow_WhenCapacityExceeded_ShouldExpand()
        {
            var initialCapacity = _storage.Capacity;

            // Rent all available rows
            for (int i = 0; i < initialCapacity; i++)
            {
                _storage.RentRow();
            }

            // This should trigger expansion
            var newRow = _storage.RentRow();
            Assert.That(_storage.Capacity, Is.GreaterThan(initialCapacity));
            Assert.That(newRow, Is.GreaterThanOrEqualTo(initialCapacity));
        }

        [Test]
        public void ReleaseRow_ShouldMakeRowAvailableAgain()
        {
            var row = _storage.RentRow();
            _storage.ReleaseRow(row);
            var newRow = _storage.RentRow();
            Assert.That(newRow, Is.EqualTo(row));
        }

        [Test]
        public void ReleaseRow_WithInvalidRow_ShouldThrowException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _storage.ReleaseRow(_storage.Capacity + 1));
        }

        [Test]
        public unsafe void GetStorage_ShouldReturnCorrectSliceForType()
        {
            var intType = StorageType.Create<int>();
            var floatType = StorageType.Create<float>();
            var vector3Type = StorageType.Create<Vector3>();

            var intStorage = _storage.GetStorage(intType);
            var floatStorage = _storage.GetStorage(floatType);
            var vector3Storage = _storage.GetStorage(vector3Type);

            Assert.That(intStorage.Length, Is.EqualTo(sizeof(int) * _storage.Capacity));
            Assert.That(floatStorage.Length, Is.EqualTo(sizeof(float) * _storage.Capacity));
            Assert.That(vector3Storage.Length, Is.EqualTo(sizeof(Vector3) * _storage.Capacity));
        }

        [Test]
        public void GetStorage_WithInvalidType_ShouldThrowException()
        {
            var invalidType = StorageType.Create<string>(); // Reference type
            LogAssert.Expect(LogType.Assert, "Assertion failed");
            Assert.Throws<ArgumentException>(() => _storage.GetStorage(invalidType));
        }

        [Test]
        public void Storage_ShouldPersistDataBetweenExpansions()
        {
            var intType = StorageType.Create<int>();
            var row = _storage.RentRow();

            // Write data
            var intStorage = _storage.GetStorage(intType);
            unsafe
            {
                var ptr = (int*)((byte*)intStorage.GetUnsafePtr() + row * sizeof(int));
                *ptr = 42;
            }

            // Force expansion
            var initialCapacity = _storage.Capacity;
            _storage.EnsureCapacity(initialCapacity * 2);

            // Verify data persists
            intStorage = _storage.GetStorage(intType);
            unsafe
            {
                var ptr = (int*)((byte*)intStorage.GetUnsafePtr() + row * sizeof(int));
                Assert.That(*ptr, Is.EqualTo(42));
            }
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(NativeTableStorage.ExpandCapacity))]
        static int CustomExpand(int current, int expect, int entitySize) => current * 2;

        [Test]
        public void CustomExpandCapacityFunction_ShouldBeRespected()
        {
            var customExpandPtr = BurstCompiler.CompileFunctionPointer<NativeTableStorage.ExpandCapacity>(CustomExpand);

            using var customStorage = new TTableStorage(0, _types, Allocator.Temp) { ExpandCapacityFunc = customExpandPtr };
            var initialCapacity = customStorage.Capacity;

            // Fill up to capacity
            for (int i = 0; i < initialCapacity; i++)
            {
                customStorage.RentRow();
            }

            // Trigger expansion
            customStorage.RentRow();

            Assert.That(customStorage.Capacity, Is.EqualTo(initialCapacity * 2));
        }

        [Test]
        public void EnsureCapacity_WithNegativeValue_ShouldThrowException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _storage.EnsureCapacity(-1));
        }

        [Test]
        public void MultipleComponentTypes_ShouldHaveCorrectLayout()
        {
            var intType = StorageType.Create<int>();
            var floatType = StorageType.Create<float>();
            var vector3Type = StorageType.Create<Vector3>();

            var row = _storage.RentRow();

            // Write test data
            unsafe
            {
                var intStorage = _storage.GetStorage(intType);
                var floatStorage = _storage.GetStorage(floatType);
                var vector3Storage = _storage.GetStorage(vector3Type);

                var intPtr = (int*)((byte*)intStorage.GetUnsafePtr() + row * sizeof(int));
                var floatPtr = (float*)((byte*)floatStorage.GetUnsafePtr() + row * sizeof(float));
                var vector3Ptr = (Vector3*)((byte*)vector3Storage.GetUnsafePtr() + row * sizeof(Vector3));

                *intPtr = 42;
                *floatPtr = 3.14f;
                *vector3Ptr = new Vector3(1, 2, 3);

                // Verify data
                Assert.That(*intPtr, Is.EqualTo(42));
                Assert.That(*floatPtr, Is.EqualTo(3.14f));
                Assert.That(*vector3Ptr, Is.EqualTo(new Vector3(1, 2, 3)));
            }
        }

        [Test]
        public void Dispose_ShouldReleaseAllResources()
        {
            var storage = new TTableStorage(0, _types, Allocator.Temp);
            storage.Dispose();

            // Try to use disposed storage
            Assert.Throws<ObjectDisposedException>(() =>
            {
                var intType = StorageType.Create<int>();
                storage.GetStorage(intType);
            });
        }

        [Test]
        public void ConcurrentAccess_ShouldBeThreadSafe()
        {
            var intType = StorageType.Create<int>();
            var rows = new NativeArray<int>(100, Allocator.TempJob);

            try
            {
                // Rent rows
                for (int i = 0; i < rows.Length; i++)
                {
                    rows[i] = _storage.RentRow();
                }

                // Create parallel writer job
                var writeJob = new WriteIntJob
                {
                    Storage = _storage.GetStorage(intType),
                    Rows = rows
                };

                // Create parallel reader job
                var readJob = new ReadIntJob
                {
                    Storage = _storage.GetStorage(intType),
                    Rows = rows,
                    Results = new NativeArray<int>(rows.Length, Allocator.TempJob)
                };

                // Execute jobs
                var writeHandle = writeJob.Schedule(rows.Length, 32);
                var readHandle = readJob.Schedule(rows.Length, 32, writeHandle);
                readHandle.Complete();

                // Verify results
                for (int i = 0; i < rows.Length; i++)
                {
                    Assert.That(readJob.Results[i], Is.EqualTo(i));
                }

                readJob.Results.Dispose();
            }
            finally
            {
                rows.Dispose();
            }
        }

        [BurstCompile]
        private struct WriteIntJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeSlice<byte> Storage;
            [ReadOnly] public NativeArray<int> Rows;

            public void Execute(int index)
            {
                unsafe
                {
                    var ptr = (int*)((byte*)Storage.GetUnsafePtr() + Rows[index] * sizeof(int));
                    *ptr = index;
                }
            }
        }

        [BurstCompile]
        private struct ReadIntJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeSlice<byte> Storage;
            [ReadOnly] public NativeArray<int> Rows;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> Results;

            public void Execute(int index)
            {
                unsafe
                {
                    var ptr = (int*)((byte*)Storage.GetUnsafePtr() + Rows[index] * sizeof(int));
                    Results[index] = *ptr;
                }
            }
        }
    }
}

#endif
