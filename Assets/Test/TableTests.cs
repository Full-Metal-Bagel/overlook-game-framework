using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace RelEcs.Tests
{
    [TestFixture]
    public class TableTests
    {
        private Lazy<Table> _lazyTable;
        private Table _table => _lazyTable.Value;
        private Archetypes _mockArchetypes; // Assuming you have a way to mock or stub this.
        private SortedSet<StorageType> _mockTypes; // Assuming you have a way to mock or stub this.

        [SetUp]
        public void SetUp()
        {
            // Initialize your mock objects here
            _mockArchetypes = new Archetypes(); // Replace with actual mock or stub.
            _mockTypes = new SortedSet<StorageType>(); // Replace with actual mock or stub.
            _lazyTable = new Lazy<Table>(() => new Table(1, _mockArchetypes, _mockTypes));
        }

        [Test]
        public void Constructor_WithValidParameters_InitializesTable()
        {
            Assert.That(_table, Is.Not.Null);
            Assert.That(_table.Id, Is.EqualTo(1));
            // Add more assertions as needed
        }

        [Test]
        public void Add_WithValidIdentity_IncreasesCount()
        {
            var identity = new Identity(1); // Replace with actual identity or mock.
            _table.Add(identity);

            Assert.That(_table.Count, Is.EqualTo(1));
        }

        [Test]
        public void Remove_WithValidRow_DecreasesCount()
        {
            var identity = new Identity(1); // Replace with actual identity or mock.
            _table.Add(identity);
            _table.Remove(0);

            Assert.That(_table.Count, Is.EqualTo(0));
        }

        [Test]
        public void Remove_WithInvalidRow_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _table.Remove(1));
        }

        [Test]
        public void GetTableEdge_WithValidStorageType_ReturnsTableEdge()
        {
            var storageType = StorageType.Create<int>(); // Replace with actual StorageType or mock.
            var result = _table.GetTableEdge(storageType);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<TableEdge>());
        }

        // Continue with other tests...
        // ... [previous code]

        [Test]
        public void GetStorage_WithValidIdentityAndType_ReturnsArray()
        {
            var identity = new Identity(1); // Replace with actual identity or mock.
            var storageType = StorageType.Create<int>(identity); // Replace with actual StorageType or mock.
            _mockTypes.Add(storageType); // Assuming SortedSet supports Add method.
            var result = _table.GetStorage<int>(identity);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<int[]>());
        }

        [Test]
        public void GetStorage_WithInvalidIdentity_ThrowsKeyNotFoundException()
        {
            var invalidIdentity = new Identity(999); // Replace with an invalid identity or mock.

            Assert.Throws<KeyNotFoundException>(() => _table.GetStorage<int>(invalidIdentity));
        }

        [Test]
        public void EnsureCapacity_WithValidCapacity_ResizesTable()
        {
            _table.EnsureCapacity(10); // Assuming this method is public.

            Assert.That(_table.Identities.Length, Is.GreaterThanOrEqualTo(10));
        }

        [Test]
        public void EnsureCapacity_WithInvalidCapacity_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _table.EnsureCapacity(-1));
        }

        [Test]
        public void MoveEntry_WithValidParameters_MovesEntryToNewTable()
        {
            var identity = new Identity(1); // Replace with actual identity or mock.
            var newTable = new Table(2, _mockArchetypes, _mockTypes);

            _table.Add(identity);
            Table.MoveEntry(identity, 0, _table, newTable);

            Assert.That(_table.Count, Is.EqualTo(0));
            Assert.That(newTable.Count, Is.EqualTo(1));
        }

        [Test]
        public void MoveEntry_WithInvalidOldRow_ThrowsArgumentOutOfRangeException()
        {
            var identity = new Identity(1); // Replace with actual identity or mock.
            var newTable = new Table(2, _mockArchetypes, _mockTypes);

            Assert.Throws<ArgumentOutOfRangeException>(() => Table.MoveEntry(identity, 1, _table, newTable));
        }

        [Test]
        public void ToString_ReturnsExpectedStringRepresentation()
        {
            var result = _table.ToString();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Contains("Table 1"), Is.True); // Adjust based on the actual ToString implementation.
        }
    }
}

