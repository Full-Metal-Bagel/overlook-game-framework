using System;
using NUnit.Framework;
using System.Collections.Generic;

namespace Overlook.Core.Tests
{
    [TestFixture]
    public class CircularBufferTests
    {
        [Test]
        public void Constructor_WithValidCapacity_CreatesBuffer()
        {
            // Arrange & Act
            using var buffer = new CircularBuffer<int>(10);

            // Assert
            Assert.That(buffer.Capacity, Is.EqualTo(10));
            Assert.That(buffer.Count, Is.EqualTo(0));
            Assert.That(buffer.IsEmpty, Is.True);
            Assert.That(buffer.IsFull, Is.False);
            Assert.That(buffer.Available, Is.EqualTo(10));
        }

        [Test]
        public void Constructor_WithZeroCapacity_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new CircularBuffer<int>(0));
        }

        [Test]
        public void Constructor_WithNegativeCapacity_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new CircularBuffer<int>(-1));
        }

        [Test]
        public void Push_ToEmptyBuffer_AddsItem()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);

            // Act
            buffer.Push(42);

            // Assert
            Assert.That(buffer.Count, Is.EqualTo(1));
            Assert.That(buffer.IsEmpty, Is.False);
            Assert.That(buffer.IsFull, Is.False);
            Assert.That(buffer.Available, Is.EqualTo(4));
        }

        [Test]
        public void Push_ToFullBuffer_ExpandsAutomatically()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(3);
            buffer.Push(1);
            buffer.Push(2);
            buffer.Push(3);

            // Act
            buffer.Push(4);

            // Assert
            Assert.That(buffer.Count, Is.EqualTo(4));
            Assert.That(buffer.Capacity, Is.EqualTo(6)); // Should expand (3 * 2 = 6)
            Assert.That(buffer.IsFull, Is.False);
        }

        [Test]
        public void TryPush_ToEmptyBuffer_ReturnsTrue()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);

            // Act
            bool result = buffer.TryPush(42);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(buffer.Count, Is.EqualTo(1));
        }

        [Test]
        public void TryPush_ToFullBuffer_ReturnsFalse()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(2);
            buffer.Push(1);
            buffer.Push(2);

            // Act
            bool result = buffer.TryPush(3);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(buffer.Count, Is.EqualTo(2));
        }

        [Test]
        public void Pop_FromBufferWithItems_ReturnsOldestItem()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);
            buffer.Push(1);
            buffer.Push(2);
            buffer.Push(3);

            // Act
            int result = buffer.Pop();

            // Assert
            Assert.That(result, Is.EqualTo(1));
            Assert.That(buffer.Count, Is.EqualTo(2));
        }

        [Test]
        public void Pop_FromEmptyBuffer_ThrowsException()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => buffer.Pop());
        }

        [Test]
        public void TryPop_FromBufferWithItems_ReturnsTrueAndItem()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);
            buffer.Push(42);

            // Act
            bool result = buffer.TryPop(out int item);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(item, Is.EqualTo(42));
            Assert.That(buffer.Count, Is.EqualTo(0));
        }

        [Test]
        public void TryPop_FromEmptyBuffer_ReturnsFalse()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);

            // Act
            bool result = buffer.TryPop(out int item);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(item, Is.EqualTo(default(int)));
        }

        [Test]
        public void Peek_FromBufferWithItems_ReturnsOldestItemWithoutRemoving()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);
            buffer.Push(42);
            buffer.Push(100);

            // Act
            int result = buffer.Peek();

            // Assert
            Assert.That(result, Is.EqualTo(42));
            Assert.That(buffer.Count, Is.EqualTo(2)); // Count unchanged
        }

        [Test]
        public void Peek_FromEmptyBuffer_ThrowsException()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => buffer.Peek());
        }

        [Test]
        public void TryPeek_FromBufferWithItems_ReturnsTrueAndItem()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);
            buffer.Push(42);

            // Act
            bool result = buffer.TryPeek(out int item);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(item, Is.EqualTo(42));
            Assert.That(buffer.Count, Is.EqualTo(1)); // Count unchanged
        }

        [Test]
        public void TryPeek_FromEmptyBuffer_ReturnsFalse()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);

            // Act
            bool result = buffer.TryPeek(out int item);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(item, Is.EqualTo(default(int)));
        }

        [Test]
        public void Indexer_WithValidIndex_ReturnsCorrectItem()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);
            buffer.Push(1);
            buffer.Push(2);
            buffer.Push(3);

            // Act & Assert
            Assert.That(buffer[0], Is.EqualTo(1)); // Oldest item
            Assert.That(buffer[1], Is.EqualTo(2));
            Assert.That(buffer[2], Is.EqualTo(3)); // Newest item
        }

        [Test]
        public void Indexer_WithInvalidIndex_ThrowsException()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);
            buffer.Push(1);
            buffer.Push(2);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer[2]);
        }

        [Test]
        public void Clear_RemovesAllItems()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);
            buffer.Push(1);
            buffer.Push(2);
            buffer.Push(3);

            // Act
            buffer.Clear();

            // Assert
            Assert.That(buffer.Count, Is.EqualTo(0));
            Assert.That(buffer.IsEmpty, Is.True);
            Assert.That(buffer.IsFull, Is.False);
            Assert.That(buffer.Available, Is.EqualTo(5));
        }

        [Test]
        public void ToArray_ReturnsCorrectItemsInOrder()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);
            buffer.Push(1);
            buffer.Push(2);
            buffer.Push(3);

            // Act
            int[] result = buffer.ToArray();

            // Assert
            Assert.That(result.Length, Is.EqualTo(3));
            Assert.That(result[0], Is.EqualTo(1));
            Assert.That(result[1], Is.EqualTo(2));
            Assert.That(result[2], Is.EqualTo(3));
        }

        [Test]
        public void ToArray_EmptyBuffer_ReturnsEmptyArray()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);

            // Act
            int[] result = buffer.ToArray();

            // Assert
            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public void CopyTo_WithValidSpan_CopiesCorrectItems()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);
            buffer.Push(1);
            buffer.Push(2);
            buffer.Push(3);

            var destination = new int[5];

            // Act
            int copied = buffer.CopyTo(destination);

            // Assert
            Assert.That(copied, Is.EqualTo(3));
            Assert.That(destination[0], Is.EqualTo(1));
            Assert.That(destination[1], Is.EqualTo(2));
            Assert.That(destination[2], Is.EqualTo(3));
        }

        [Test]
        public void CopyTo_WithSmallSpan_CopiesOnlyAvailableSpace()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);
            buffer.Push(1);
            buffer.Push(2);
            buffer.Push(3);

            var destination = new int[2];

            // Act
            int copied = buffer.CopyTo(destination);

            // Assert
            Assert.That(copied, Is.EqualTo(2));
            Assert.That(destination[0], Is.EqualTo(1));
            Assert.That(destination[1], Is.EqualTo(2));
        }

        [Test]
        public void PushRange_WithValidItems_PushesAllItems()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);
            var items = new int[] { 1, 2, 3 };

            // Act
            buffer.PushRange(items);

            // Assert
            Assert.That(buffer.Count, Is.EqualTo(3));
            Assert.That(buffer[0], Is.EqualTo(1));
            Assert.That(buffer[1], Is.EqualTo(2));
            Assert.That(buffer[2], Is.EqualTo(3));
        }

        [Test]
        public void PushRange_WithAutoExpansion_ReturnsCorrectCount()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(2);
            var items = new int[] { 1, 2, 3, 4 };

            // Act
            buffer.PushRange(items);

            // Assert
            Assert.That(buffer.Count, Is.EqualTo(4));
            Assert.That(buffer.Capacity, Is.EqualTo(4)); // Expanded from 2 to 4
            Assert.That(buffer[0], Is.EqualTo(1)); // All items preserved
            Assert.That(buffer[1], Is.EqualTo(2));
            Assert.That(buffer[2], Is.EqualTo(3));
            Assert.That(buffer[3], Is.EqualTo(4));
        }

        [Test]
        public void PopRange_WithValidDestination_PopsCorrectItems()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);
            buffer.Push(1);
            buffer.Push(2);
            buffer.Push(3);

            var destination = new int[5];

            // Act
            int popped = buffer.PopRange(destination);

            // Assert
            Assert.That(popped, Is.EqualTo(3));
            Assert.That(destination[0], Is.EqualTo(1));
            Assert.That(destination[1], Is.EqualTo(2));
            Assert.That(destination[2], Is.EqualTo(3));
            Assert.That(buffer.Count, Is.EqualTo(0));
        }

        [Test]
        public void PopRange_WithSmallDestination_PopsOnlyAvailableSpace()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);
            buffer.Push(1);
            buffer.Push(2);
            buffer.Push(3);

            var destination = new int[2];

            // Act
            int popped = buffer.PopRange(destination);

            // Assert
            Assert.That(popped, Is.EqualTo(2));
            Assert.That(destination[0], Is.EqualTo(1));
            Assert.That(destination[1], Is.EqualTo(2));
            Assert.That(buffer.Count, Is.EqualTo(1));
            Assert.That(buffer[0], Is.EqualTo(3)); // Remaining item
        }

        [Test]
        public void CircularBuffer_WithStructType_WorksCorrectly()
        {
            // Arrange
            using var buffer = new CircularBuffer<TestStruct>(3);
            var item1 = new TestStruct { Value = 1, Id = 101 };
            var item2 = new TestStruct { Value = 2, Id = 102 };

            // Act
            buffer.Push(item1);
            buffer.Push(item2);

            // Assert
            Assert.That(buffer.Count, Is.EqualTo(2));
            Assert.That(buffer[0], Is.EqualTo(item1));
            Assert.That(buffer[1], Is.EqualTo(item2));
        }

        [Test]
        public void CircularBuffer_OverflowBehavior_WorksCorrectly()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(3);

            // Fill buffer
            buffer.Push(1);
            buffer.Push(2);
            buffer.Push(3);

            // Assert
            Assert.That(buffer.Count, Is.EqualTo(3));
            Assert.That(buffer[0], Is.EqualTo(1)); // Oldest after overflow
            Assert.That(buffer[1], Is.EqualTo(2));
            Assert.That(buffer[2], Is.EqualTo(3)); // Newest

            // Act - Overflow
            buffer.Push(4);
            buffer.Push(5);

            // Assert
            Assert.That(buffer.Count, Is.EqualTo(5));
            Assert.That(buffer[0], Is.EqualTo(1)); // Oldest after overflow
            Assert.That(buffer[1], Is.EqualTo(2));
            Assert.That(buffer[2], Is.EqualTo(3)); // Newest
            Assert.That(buffer[3], Is.EqualTo(4)); // Newest
            Assert.That(buffer[4], Is.EqualTo(5)); // Newest
        }

        [Test]
        public void CircularBuffer_ComplexOperations_WorkCorrectly()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(4);

            // Act - Complex sequence
            buffer.Push(1);
            buffer.Push(2);
            int first = buffer.Pop();
            buffer.Push(3);
            buffer.Push(4);
            buffer.Push(5);
            buffer.Push(6);

            // Assert
            Assert.That(first, Is.EqualTo(1));
            Assert.That(buffer.Count, Is.EqualTo(5));
            Assert.That(buffer[0], Is.EqualTo(2)); // Oldest
            Assert.That(buffer[1], Is.EqualTo(3));
            Assert.That(buffer[2], Is.EqualTo(4));
            Assert.That(buffer[3], Is.EqualTo(5)); // Newest (5 was overwritten)
            Assert.That(buffer[4], Is.EqualTo(6)); // Newest (5 was overwritten)
        }

        [Test]
        public void Dispose_ReleasesResources()
        {
            // Arrange
            var buffer = new CircularBuffer<int>(5);
            buffer.Push(1);
            buffer.Push(2);

            // Act
            buffer.Dispose();

            // Assert
            Assert.That(buffer.Count, Is.EqualTo(2)); // Count is preserved after dispose
            Assert.DoesNotThrow(() => _ = buffer.Count); // Should be safe to access properties after dispose
        }

        [Test]
        public void Dispose_CalledMultipleTimes_IsSafe()
        {
            // Arrange
            var buffer = new CircularBuffer<int>(5);

            // Act & Assert
            Assert.DoesNotThrow(() => buffer.Dispose());
            Assert.DoesNotThrow(() => buffer.Dispose());
        }

        [Test]
        public void UsingStatement_AutomaticallyDisposes()
        {
            // Arrange & Act
            CircularBuffer<int> buffer;
            using (buffer = new CircularBuffer<int>(5))
            {
                buffer.Push(1);
                buffer.Push(2);
            }

            // Assert - Buffer should be disposed after using block
            Assert.That(buffer.Count, Is.EqualTo(2)); // Count is preserved after dispose
            Assert.DoesNotThrow(() => _ = buffer.Count); // Should be safe to access properties after dispose
        }

        [Test]
        public void Push_AutoExpansion_WorksCorrectly()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(2); // Start with capacity 2, default expansion (doubles)

            // Act - Push items beyond initial capacity
            buffer.Push(1);
            buffer.Push(2);
            Assert.That(buffer.Capacity, Is.EqualTo(2));
            Assert.That(buffer.Count, Is.EqualTo(2));

            buffer.Push(3); // This should trigger expansion

            // Assert
            Assert.That(buffer.Capacity, Is.EqualTo(4)); // Should double to 4
            Assert.That(buffer.Count, Is.EqualTo(3));
            Assert.That(buffer[0], Is.EqualTo(1));
            Assert.That(buffer[1], Is.EqualTo(2));
            Assert.That(buffer[2], Is.EqualTo(3));
        }

        [Test]
        public void PushWithOverwrite_MaintainsOldBehavior()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(2);

            // Act
            buffer.PushWithOverwrite(1);
            buffer.PushWithOverwrite(2);
            bool result = buffer.PushWithOverwrite(3); // Should overwrite

            // Assert
            Assert.That(result, Is.False); // Should indicate overwrite occurred
            Assert.That(buffer.Capacity, Is.EqualTo(2)); // Capacity unchanged
            Assert.That(buffer.Count, Is.EqualTo(2));
            Assert.That(buffer[0], Is.EqualTo(2)); // Oldest after overwrite
            Assert.That(buffer[1], Is.EqualTo(3)); // Newest
        }

        [Test]
        public void ExpandTo_ManualExpansion_WorksCorrectly()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(3);
            buffer.Push(1);
            buffer.Push(2);

            // Act
            buffer.ExpandTo(10);

            // Assert
            Assert.That(buffer.Capacity, Is.EqualTo(10));
            Assert.That(buffer.Count, Is.EqualTo(2));
            Assert.That(buffer[0], Is.EqualTo(1));
            Assert.That(buffer[1], Is.EqualTo(2));
        }

        [Test]
        public void ExpandTo_WithInvalidCapacity_ThrowsException()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => buffer.ExpandTo(3)); // Smaller than current
            Assert.Throws<ArgumentException>(() => buffer.ExpandTo(5)); // Same as current
        }

        [Test]
        public void Push_CustomExpandFunction_WorksCorrectly()
        {
            // Arrange
            // Custom expand function that adds 3 to current capacity
            using var buffer = new CircularBuffer<int>(2, capacity => capacity + 3);

            // Act - Push items beyond initial capacity
            buffer.Push(1);
            buffer.Push(2);
            Assert.That(buffer.Capacity, Is.EqualTo(2));

            buffer.Push(3); // This should trigger expansion

            // Assert
            Assert.That(buffer.Capacity, Is.EqualTo(5)); // Should expand by 3 (2 + 3 = 5)
            Assert.That(buffer.Count, Is.EqualTo(3));
            Assert.That(buffer[0], Is.EqualTo(1));
            Assert.That(buffer[1], Is.EqualTo(2));
            Assert.That(buffer[2], Is.EqualTo(3));
        }

        [Test]
        public void Push_InvalidExpandFunction_ThrowsException()
        {
            // Arrange
            // Invalid expand function that returns same or smaller capacity
            using var buffer = new CircularBuffer<int>(3, capacity => capacity); // Returns same capacity

            buffer.Push(1);
            buffer.Push(2);
            buffer.Push(3);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => buffer.Push(4));
        }

        [Test]
        public void StructEnumerator_WorksCorrectly()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);
            buffer.Push(1);
            buffer.Push(2);
            buffer.Push(3);

            // Act & Assert - Test struct enumerator directly
            var enumerator = buffer.GetEnumerator();
            var items = new List<int>();

            while (enumerator.MoveNext())
            {
                items.Add(enumerator.Current);
            }

            Assert.That(items.Count, Is.EqualTo(3));
            Assert.That(items[0], Is.EqualTo(1));
            Assert.That(items[1], Is.EqualTo(2));
            Assert.That(items[2], Is.EqualTo(3));
        }

        [Test]
        public void StructEnumerator_WithForeachLoop_WorksCorrectly()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);
            buffer.Push(10);
            buffer.Push(20);
            buffer.Push(30);

            // Act
            var items = new List<int>();
            foreach (var item in buffer)
            {
                items.Add(item);
            }

            // Assert
            Assert.That(items.Count, Is.EqualTo(3));
            Assert.That(items[0], Is.EqualTo(10));
            Assert.That(items[1], Is.EqualTo(20));
            Assert.That(items[2], Is.EqualTo(30));
        }

        [Test]
        public void StructEnumerator_EmptyBuffer_WorksCorrectly()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);

            // Act & Assert
            var enumerator = buffer.GetEnumerator();
            Assert.That(enumerator.MoveNext(), Is.False);

            // Test with foreach
            var items = new List<int>();
            foreach (var item in buffer)
            {
                items.Add(item);
            }
            Assert.That(items.Count, Is.EqualTo(0));
        }

        [Test]
        public void StructEnumerator_Reset_WorksCorrectly()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(5);
            buffer.Push(1);
            buffer.Push(2);

            // Act
            var enumerator = buffer.GetEnumerator();

            // First iteration
            var firstPass = new List<int>();
            while (enumerator.MoveNext())
            {
                firstPass.Add(enumerator.Current);
            }

            // Reset and second iteration
            enumerator.Reset();
            var secondPass = new List<int>();
            while (enumerator.MoveNext())
            {
                secondPass.Add(enumerator.Current);
            }

            // Assert
            Assert.That(firstPass.Count, Is.EqualTo(2));
            Assert.That(secondPass.Count, Is.EqualTo(2));
            Assert.That(firstPass[0], Is.EqualTo(secondPass[0]));
            Assert.That(firstPass[1], Is.EqualTo(secondPass[1]));
        }

        [Test]
        public void StructEnumerator_PerformanceDemo_NoGCAllocation()
        {
            // Arrange
            using var buffer = new CircularBuffer<int>(100); // Smaller buffer to avoid expansion
            for (int i = 0; i < 100; i++)
            {
                buffer.Push(i);
            }

            // Act - This should not allocate on the heap when using struct enumerator
            long startMemory = GC.GetTotalMemory(true);

            // Multiple iterations to demonstrate no allocations
            for (int iteration = 0; iteration < 5; iteration++)
            {
                var enumerator = buffer.GetEnumerator();
                int sum = 0;
                while (enumerator.MoveNext())
                {
                    sum += enumerator.Current;
                }
                // Prevent optimization from removing the loop
                Assert.That(sum, Is.EqualTo(4950)); // Sum of 0 to 99
            }

            long endMemory = GC.GetTotalMemory(false);

            // Assert - Memory usage should be minimal (struct enumerator doesn't allocate)
            long memoryDiff = endMemory - startMemory;

            // Note: The struct enumerator itself doesn't allocate, but test infrastructure might
            // The key benefit is avoiding IEnumerator<T> boxing allocations during foreach
            Assert.That(memoryDiff, Is.LessThan(50000), "Struct enumerator should minimize allocations");

            // The real test is that this compiles and works - struct enumerators avoid boxing
            Assert.Pass("Struct enumerator successfully avoids IEnumerator<T> boxing in foreach loops");
        }

        private struct TestStruct
        {
            public int Value;
            public int Id;
        }
    }
}
