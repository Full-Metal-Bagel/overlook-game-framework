using NUnit.Framework;
using System;
#if OVERLOOK_ECS_USE_UNITY_COLLECTION
using TSet = Overlook.Ecs.NativeBitArraySet;
#else
using TSet = Overlook.Ecs.SortedSetTypeSet;
#endif

namespace Overlook.Ecs.Tests
{
    [TestFixture]
    public class TableTests
    {
        private Lazy<Table> _lazyTable;
        private Table _table => _lazyTable.Value;
        private Archetypes _mockArchetypes; // Assuming you have a way to mock or stub this.
        private TableStorage _tableStorage;
        private TSet _mockTypes; // Assuming you have a way to mock or stub this.

        [SetUp]
        public void SetUp()
        {
            // Initialize your mock objects here
            _mockArchetypes = new Archetypes(); // Replace with actual mock or stub.
            _mockTypes = TSet.Create(); // Replace with actual mock or stub.
            _tableStorage = new TableStorage(_mockTypes);
            _lazyTable = new Lazy<Table>(() => new Table(1, _mockTypes, _tableStorage));
        }

        [TearDown]
        public void TearDown()
        {
            _mockArchetypes.Dispose();
            _mockTypes.Dispose();
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
            var row = _table.Add(identity);
            _table.Remove(row);

            Assert.That(_table.Count, Is.EqualTo(0));
        }

        [Test]
        public void Remove_WithInvalidRow_ThrowsException()
        {
            AssertUtils.ExpectDebugAssert(() => _table.Remove(1), assertionTimes: 2);
        }

        [Test]
        public void EnsureCapacity_WithValidCapacity_ResizesTable()
        {
            _tableStorage.EnsureCapacity(10); // Assuming this method is public.

            Assert.That(_tableStorage.Capacity, Is.GreaterThanOrEqualTo(10));
        }

        [Test]
        public void EnsureCapacity_WithInvalidCapacity_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _tableStorage.EnsureCapacity(-1));
        }

        [Test]
        public void MoveEntry_WithValidParameters_MovesEntryToNewTable()
        {
            var identity = new Identity(1); // Replace with actual identity or mock.
            var newTable = new Table(2, _mockTypes, _tableStorage);

            var row = _table.Add(identity);
            Table.MoveEntry(identity, row, _table, newTable);

            Assert.That(_table.Count, Is.EqualTo(0));
            Assert.That(newTable.Count, Is.EqualTo(1));
        }

        [Test]
        public void MoveEntry_WithInvalidOldRow_ThrowsException()
        {
            var identity = new Identity(1); // Replace with actual identity or mock.
            var newTable = new Table(2, _mockTypes, _tableStorage);

            AssertUtils.ExpectDebugAssert(() => Table.MoveEntry(identity, 999, _table, newTable));
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

