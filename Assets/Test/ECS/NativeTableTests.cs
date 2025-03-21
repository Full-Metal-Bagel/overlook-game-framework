#if ARCHETYPE_USE_UNITY_NATIVE_COLLECTION

using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using TTableStorage = RelEcs.NativeTableStorage<RelEcs.Fixed8Bytes>;

namespace RelEcs.Tests
{
    public struct TestComponent : IEquatable<TestComponent>
    {
        public int Value;
        public float FloatValue;

        public bool Equals(TestComponent other)
        {
            return Value == other.Value && Math.Abs(FloatValue - other.FloatValue) < 0.001f;
        }

        public override bool Equals(object obj)
        {
            return obj is TestComponent other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, FloatValue);
        }
    }

    public struct TestComponent2 : IEquatable<TestComponent2>
    {
        public byte ByteValue;
        public long LongValue;

        public bool Equals(TestComponent2 other)
        {
            return ByteValue == other.ByteValue && LongValue == other.LongValue;
        }

        public override bool Equals(object obj)
        {
            return obj is TestComponent2 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ByteValue, LongValue);
        }
    }

    [TestFixture]
    public unsafe class NativeTableTests
    {
        private NativeTableStorage<Fixed8Bytes> _tableStorage;
        private NativeBitArraySet _types;
        private NativeTable _table;

        [SetUp]
        public void Setup()
        {
            // Create a set of component types for the table
            _types = NativeBitArraySet.Create();
            _types.Add(StorageType.Create<TestComponent>());
            _types.Add(StorageType.Create<TestComponent2>());

            // Create table storage
            _tableStorage = new NativeTableStorage<Fixed8Bytes>(1, _types, Allocator.Temp);

            // Create the table
            _table = new NativeTable(1, _types, (TTableStorage*)UnsafeUtility.AddressOf(ref _tableStorage), Allocator.Temp);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up resources
            _table.Dispose();
            _tableStorage.Dispose();
        }

        [Test]
        public void Constructor_InitializesTableCorrectly()
        {
            // Assert
            Assert.AreEqual(1, _table.Id);
            Assert.AreEqual(_types.Length, _table.Types.Length);
            Assert.AreEqual(0, _table.Count);
            Assert.IsTrue(_table.IsEmpty);

            // Check that all expected types are in the table
            Assert.IsTrue(_table.Types.Contains(StorageType.Create<TestComponent>()));
            Assert.IsTrue(_table.Types.Contains(StorageType.Create<TestComponent2>()));
        }

        [Test]
        public void Add_ReturnsValidRowIndex()
        {
            // Act
            var identity = new Identity(1, 1);
            int row = _table.Add(identity);

            // Assert
            Assert.That(row, Is.GreaterThanOrEqualTo(0));
            Assert.AreEqual(1, _table.Count);
            Assert.IsFalse(_table.IsEmpty);
        }

        [Test]
        public void Remove_ReducesCount()
        {
            // Arrange
            var identity = new Identity(1, 1);
            int row = _table.Add(identity);

            // Act
            _table.Remove(row);

            // Assert
            Assert.AreEqual(0, _table.Count);
            Assert.IsTrue(_table.IsEmpty);
        }

        [Test]
        public void GetStorage_ReturnsCorrectStorage()
        {
            // Arrange
            var identity = new Identity(1, 1);
            int row = _table.Add(identity);

            var testComponent = new TestComponent { Value = 42, FloatValue = 3.14f };

            // Act
            var storage = _table.GetStorage<TestComponent>();
            UnsafeUtility.WriteArrayElement(storage.GetUnsafePtr(), row, testComponent);

            // Assert
            var retrievedComponent = UnsafeUtility.ReadArrayElement<TestComponent>(storage.GetUnsafePtr(), row);
            Assert.AreEqual(testComponent.Value, retrievedComponent.Value);
            Assert.AreEqual(testComponent.FloatValue, retrievedComponent.FloatValue);
        }

        [Test]
        public void MoveEntry_SameTable_ReturnsOriginalRow()
        {
            // Arrange
            var identity = new Identity(1, 1);
            int row = _table.Add(identity);

            // Act
            int newRow = NativeTable.MoveEntry(identity, row, ref _table, ref _table);

            // Assert
            Assert.AreEqual(row, newRow);
        }

        [Test]
        public void MoveEntry_DifferentTables_CopiesData()
        {
            // Arrange
            var identity = new Identity(1, 1);
            int row = _table.Add(identity);

            // Write test data to the first table
            var testComponent = new TestComponent { Value = 42, FloatValue = 3.14f };
            var testComponent2 = new TestComponent2 { ByteValue = 255, LongValue = 1234567890L };

            var storage1 = _table.GetStorage<TestComponent>();
            var storage2 = _table.GetStorage<TestComponent2>();
            UnsafeUtility.WriteArrayElement(storage1.GetUnsafePtr(), row, testComponent);
            UnsafeUtility.WriteArrayElement(storage2.GetUnsafePtr(), row, testComponent2);

            // Create a second table with the same component types
            var types2 = NativeBitArraySet.Create();
            types2.Add(StorageType.Create<TestComponent>());
            types2.Add(StorageType.Create<TestComponent2>());

            var tableStorage2 = new TTableStorage(2, types2, Allocator.Temp);
            var table2 = new NativeTable(2, types2, (TTableStorage*)UnsafeUtility.AddressOf(ref tableStorage2), Allocator.Temp);

            try
            {
                // Act
                int newRow = NativeTable.MoveEntry(identity, row, ref _table, ref table2);

                // Assert
                Assert.AreEqual(0, _table.Count); // Original table should be empty
                Assert.AreEqual(1, table2.Count); // New table should have one entry

                // Check that data was copied correctly
                var newStorage1 = table2.GetStorage<TestComponent>();
                var newStorage2 = table2.GetStorage<TestComponent2>();

                var retrievedComponent1 = UnsafeUtility.ReadArrayElement<TestComponent>(newStorage1.GetUnsafePtr(), newRow);
                var retrievedComponent2 = UnsafeUtility.ReadArrayElement<TestComponent2>(newStorage2.GetUnsafePtr(), newRow);

                Assert.AreEqual(testComponent.Value, retrievedComponent1.Value);
                Assert.AreEqual(testComponent.FloatValue, retrievedComponent1.FloatValue);
                Assert.AreEqual(testComponent2.ByteValue, retrievedComponent2.ByteValue);
                Assert.AreEqual(testComponent2.LongValue, retrievedComponent2.LongValue);
            }
            finally
            {
                // Clean up
                table2.Dispose();
                types2.Dispose();
            }
        }

        [Test]
        public void MoveEntry_DifferentTablesWithSharedStorage_PreservesRow()
        {
            // Arrange
            var identity = new Identity(1, 1);
            int row = _table.Add(identity);

            // Create a second table using the same storage
            var types2 = NativeBitArraySet.Create();
            types2.Add(StorageType.Create<TestComponent>());
            types2.Add(StorageType.Create<TestComponent2>());

            var table2 = new NativeTable(2, types2, (TTableStorage*)UnsafeUtility.AddressOf(ref _tableStorage), Allocator.Temp);

            try
            {
                // Act
                int newRow = NativeTable.MoveEntry(identity, row, ref _table, ref table2);

                // Assert
                Assert.AreEqual(0, _table.Count); // Original table should be empty
                Assert.AreEqual(1, table2.Count); // New table should have one entry
            }
            finally
            {
                // Clean up
                table2.Dispose();
                types2.Dispose();
            }
        }

        [Test]
        public void MoveEntry_PartialComponentOverlap_OnlyCopiesSharedComponents()
        {
            // Arrange
            var identity = new Identity(1, 1);
            int row = _table.Add(identity);

            // Write test data to the first table
            var testComponent = new TestComponent { Value = 42, FloatValue = 3.14f };
            var testComponent2 = new TestComponent2 { ByteValue = 255, LongValue = 1234567890L };

            var storage1 = _table.GetStorage<TestComponent>();
            var storage2 = _table.GetStorage<TestComponent2>();
            UnsafeUtility.WriteArrayElement(storage1.GetUnsafePtr(), row, testComponent);
            UnsafeUtility.WriteArrayElement(storage2.GetUnsafePtr(), row, testComponent2);

            // Create a second table with only one of the component types
            var types2 = NativeBitArraySet.Create();
            types2.Add(StorageType.Create<TestComponent>()); // Only TestComponent, not TestComponent2

            var tableStorage2 = new TTableStorage(2, types2, Allocator.Temp);
            var table2 = new NativeTable(2, types2, (TTableStorage*)UnsafeUtility.AddressOf(ref tableStorage2), Allocator.Temp);

            try
            {
                // Act
                int newRow = NativeTable.MoveEntry(identity, row, ref _table, ref table2);

                // Assert
                Assert.AreEqual(0, _table.Count); // Original table should be empty
                Assert.AreEqual(1, table2.Count); // New table should have one entry

                // Check that only TestComponent was copied
                var newStorage1 = table2.GetStorage<TestComponent>();
                var retrievedComponent1 = UnsafeUtility.ReadArrayElement<TestComponent>(newStorage1.GetUnsafePtr(), newRow);

                Assert.AreEqual(testComponent.Value, retrievedComponent1.Value);
                Assert.AreEqual(testComponent.FloatValue, retrievedComponent1.FloatValue);

                // TestComponent2 doesn't exist in the new table, so no need to check it
            }
            finally
            {
                // Clean up
                table2.Dispose();
                tableStorage2.Dispose();
                types2.Dispose();
            }
        }

        [Test]
        public void Dispose_CleansUpResources()
        {
            // Arrange
            var types = NativeBitArraySet.Create();
            types.Add(StorageType.Create<TestComponent>());

            var tableStorage = new TTableStorage(999, types, Allocator.Temp);
            var table = new NativeTable(999, types, (TTableStorage*)UnsafeUtility.AddressOf(ref tableStorage), Allocator.Temp);

            // Act
            table.Dispose();
            tableStorage.Dispose();

            // Assert - no exception should be thrown
            Assert.Pass("Table was disposed without exceptions");
        }
    }
}

#endif
