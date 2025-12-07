using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Overlook.System.Tests
{
    [TestFixture]
    public class SystemEventsTests
    {
        #region SystemEvents<T> Tests

        [Test]
        public void SystemEvents_InitialState_HasZeroCounts()
        {
            // Arrange & Act
            using var events = new SystemEvents<TestEvent>();

            // Assert
            Assert.That(events.Count, Is.EqualTo(0));
            Assert.That(events.PendingCount, Is.EqualTo(0));
        }

        [Test]
        public void SystemEvents_Append_IncreasesPendingCount()
        {
            // Arrange
            using var events = new SystemEvents<TestEvent>();

            // Act
            events.Append(new TestEvent { Value = 1 });

            // Assert
            Assert.That(events.PendingCount, Is.EqualTo(1));
            Assert.That(events.Count, Is.EqualTo(0)); // Not yet ticked
        }

        [Test]
        public void SystemEvents_AppendMultiple_IncreasesPendingCount()
        {
            // Arrange
            using var events = new SystemEvents<TestEvent>();

            // Act
            events.Append(new TestEvent { Value = 1 });
            events.Append(new TestEvent { Value = 2 });
            events.Append(new TestEvent { Value = 3 });

            // Assert
            Assert.That(events.PendingCount, Is.EqualTo(3));
            Assert.That(events.Count, Is.EqualTo(0));
        }

        [Test]
        public void SystemEvents_Tick_MovesPendingToActive()
        {
            // Arrange
            using var events = new SystemEvents<TestEvent>();
            events.Append(new TestEvent { Value = 1 });
            events.Append(new TestEvent { Value = 2 });

            // Act
            events.Tick(systemIndex: 0, currentFrame: 0);

            // Assert
            Assert.That(events.PendingCount, Is.EqualTo(0));
            Assert.That(events.Count, Is.EqualTo(2));
        }

        [Test]
        public void SystemEvents_Indexer_ReturnsCorrectEvent()
        {
            // Arrange
            using var events = new SystemEvents<TestEvent>();
            events.Append(new TestEvent { Value = 42 });
            events.Append(new TestEvent { Value = 100 });
            events.Tick(systemIndex: 0, currentFrame: 0);

            // Act & Assert
            Assert.That(events[0].Value, Is.EqualTo(42));
            Assert.That(events[1].Value, Is.EqualTo(100));
        }

        [Test]
        public void SystemEvents_Indexer_ThrowsOnInvalidIndex()
        {
            // Arrange
            using var events = new SystemEvents<TestEvent>();
            events.Append(new TestEvent { Value = 1 });
            events.Tick(systemIndex: 0, currentFrame: 0);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = events[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = events[1]);
        }

        [Test]
        public void SystemEvents_EventExpiresAfterLastingFrames()
        {
            // Arrange
            using var events = new SystemEvents<TestEvent>();
            events.Append(new TestEvent { Value = 1 }, lastingFrames: 2);
            events.Tick(systemIndex: 0, currentFrame: 0);

            // Assert - Frame 0, event should exist
            Assert.That(events.Count, Is.EqualTo(1));

            // Act - Frame 1, event should still exist
            events.Tick(systemIndex: 0, currentFrame: 1);
            Assert.That(events.Count, Is.EqualTo(1));

            // Act - Frame 2, event should be removed (lasted 2 frames: 0 and 1)
            events.Tick(systemIndex: 0, currentFrame: 2);
            Assert.That(events.Count, Is.EqualTo(0));
        }

        [Test]
        public void SystemEvents_DefaultLastingFrames_IsOne()
        {
            // Arrange
            using var events = new SystemEvents<TestEvent>();
            events.Append(new TestEvent { Value = 1 }); // Default lastingFrames = 1
            events.Tick(systemIndex: 0, currentFrame: 0);

            // Assert - Frame 0, event should exist
            Assert.That(events.Count, Is.EqualTo(1));

            // Act - Frame 1, event should be removed
            events.Tick(systemIndex: 0, currentFrame: 1);
            Assert.That(events.Count, Is.EqualTo(0));
        }

        [Test]
        public void SystemEvents_MultipleEventsWithDifferentLastingFrames()
        {
            // Arrange
            using var events = new SystemEvents<TestEvent>();
            events.Append(new TestEvent { Value = 1 }, lastingFrames: 1);
            events.Append(new TestEvent { Value = 2 }, lastingFrames: 3);
            events.Tick(systemIndex: 0, currentFrame: 0);

            // Assert - Frame 0
            Assert.That(events.Count, Is.EqualTo(2));

            // Frame 1 - first event expires
            events.Tick(systemIndex: 0, currentFrame: 1);
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Value, Is.EqualTo(2));

            // Frame 2 - second event still exists
            events.Tick(systemIndex: 0, currentFrame: 2);
            Assert.That(events.Count, Is.EqualTo(1));

            // Frame 3 - second event expires
            events.Tick(systemIndex: 0, currentFrame: 3);
            Assert.That(events.Count, Is.EqualTo(0));
        }

        [Test]
        public void SystemEvents_ForEach_IteratesAllEvents()
        {
            // Arrange
            using var events = new SystemEvents<TestEvent>();
            events.Append(new TestEvent { Value = 1 });
            events.Append(new TestEvent { Value = 2 });
            events.Append(new TestEvent { Value = 3 });
            events.Tick(systemIndex: 0, currentFrame: 0);

            // Act
            var sum = 0;
            events.ForEach(e => sum += e.Value);

            // Assert
            Assert.That(sum, Is.EqualTo(6));
        }

        [Test]
        public void SystemEvents_ForEachWithData_PassesDataCorrectly()
        {
            // Arrange
            using var events = new SystemEvents<TestEvent>();
            events.Append(new TestEvent { Value = 10 });
            events.Append(new TestEvent { Value = 20 });
            events.Tick(systemIndex: 0, currentFrame: 0);

            // Act
            var result = new List<int>();
            events.ForEach(result, (list, e) => list.Add(e.Value));

            // Assert
            Assert.That(result, Is.EqualTo(new[] { 10, 20 }));
        }

        [Test]
        public void SystemEvents_GetEnumerator_IteratesAllEvents()
        {
            // Arrange
            using var events = new SystemEvents<TestEvent>();
            events.Append(new TestEvent { Value = 5 });
            events.Append(new TestEvent { Value = 10 });
            events.Tick(systemIndex: 0, currentFrame: 0);

            // Act
            var values = new List<int>();
            foreach (var e in events)
            {
                values.Add(e.Value);
            }

            // Assert
            Assert.That(values, Is.EqualTo(new[] { 5, 10 }));
        }

        [Test]
        public void SystemEvents_OnlyRemovesEventsFromSameSystemIndex()
        {
            // Arrange
            using var events = new SystemEvents<TestEvent>();

            // Add event in system 0
            events.Append(new TestEvent { Value = 1 }, lastingFrames: 1);
            events.Tick(systemIndex: 0, currentFrame: 0);

            // Add event in system 1
            events.Append(new TestEvent { Value = 2 }, lastingFrames: 1);
            events.Tick(systemIndex: 1, currentFrame: 0);

            Assert.That(events.Count, Is.EqualTo(2));

            // Tick system 0 at frame 1 - should only expire system 0's event
            events.Tick(systemIndex: 0, currentFrame: 1);
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Value, Is.EqualTo(2));

            // Tick system 1 at frame 1 - should expire system 1's event
            events.Tick(systemIndex: 1, currentFrame: 1);
            Assert.That(events.Count, Is.EqualTo(0));
        }

        [Test]
        public void SystemEvents_WithCapacity_CreatesWithSpecifiedCapacity()
        {
            // Arrange & Act
            using var events = new SystemEvents<TestEvent>(capacity: 16);

            // Assert - just verify it doesn't throw and works correctly
            events.Append(new TestEvent { Value = 1 });
            events.Tick(systemIndex: 0, currentFrame: 0);
            Assert.That(events.Count, Is.EqualTo(1));
        }

        #endregion

        #region SystemEventsManager Tests

        [Test]
        public void SystemEventsManager_Append_CreatesEventQueue()
        {
            // Arrange
            var manager = new SystemEventsManager();

            // Act
            manager.AppendEvent(new TestEvent { Value = 42 });

            // Assert
            var events = manager.GetEvents<TestEvent>();
            Assert.That(events.PendingCount, Is.EqualTo(1));
        }

        [Test]
        public void SystemEventsManager_Get_ReturnsSameInstance()
        {
            // Arrange
            var manager = new SystemEventsManager();

            // Act
            var events1 = manager.GetEvents<TestEvent>();
            var events2 = manager.GetEvents<TestEvent>();

            // Assert
            Assert.That(events1, Is.SameAs(events2));
        }

        [Test]
        public void SystemEventsManager_Tick_TicksAllEventQueues()
        {
            // Arrange
            var manager = new SystemEventsManager();
            manager.AppendEvent(new TestEvent { Value = 1 });
            manager.AppendEvent(new AnotherTestEvent { Data = 2 });

            // Act
            manager.Tick(systemIndex: 0, currentFrame: 0);

            // Assert
            var events1 = manager.GetEvents<TestEvent>();
            var events2 = manager.GetEvents<AnotherTestEvent>();
            Assert.That(events1.Count, Is.EqualTo(1));
            Assert.That(events1.PendingCount, Is.EqualTo(0));
            Assert.That(events2.Count, Is.EqualTo(1));
            Assert.That(events2.PendingCount, Is.EqualTo(0));
        }

        [Test]
        public void SystemEventsManager_Tick_ExpiresEventsCorrectly()
        {
            // Arrange
            var manager = new SystemEventsManager();
            manager.Tick(systemIndex: 0, currentFrame: 0);

            manager.AppendEvent(new TestEvent { Value = 1 }, lastingFrames: 1);
            manager.AppendEvent(new AnotherTestEvent { Data = 2 }, lastingFrames: 2);

            // Act - Frame 1
            manager.Tick(systemIndex: 0, currentFrame: 1);

            // Assert
            var events1 = manager.GetEvents<TestEvent>();
            var events2 = manager.GetEvents<AnotherTestEvent>();
            Assert.That(events1.Count, Is.EqualTo(1)); // Still active
            Assert.That(events2.Count, Is.EqualTo(1)); // Still active

            manager.Tick(systemIndex: 0, currentFrame: 2);
            Assert.That(events1.Count, Is.EqualTo(0)); // Still active
            Assert.That(events2.Count, Is.EqualTo(1)); // Still active
        }

        [Test]
        public void SystemEventsManager_MultipleAppends_WorkCorrectly()
        {
            // Arrange
            var manager = new SystemEventsManager();

            // Act
            manager.AppendEvent(new TestEvent { Value = 1 });
            manager.AppendEvent(new TestEvent { Value = 2 });
            manager.AppendEvent(new TestEvent { Value = 3 });
            manager.Tick(systemIndex: 0, currentFrame: 0);

            // Assert
            var events = manager.GetEvents<TestEvent>();
            Assert.That(events.Count, Is.EqualTo(3));
            Assert.That(events[0].Value, Is.EqualTo(1));
            Assert.That(events[1].Value, Is.EqualTo(2));
            Assert.That(events[2].Value, Is.EqualTo(3));
        }

        #endregion

        #region SystemEventAttribute Tests

        [Test]
        public void SystemEventAttribute_InitCapacity_CanBeSet()
        {
            // Arrange & Act
            var attr = new SystemEventAttribute { InitCapacity = 32 };

            // Assert
            Assert.That(attr.InitCapacity, Is.EqualTo(32));
        }

        [Test]
        public void SystemEvents_WithAttributeCapacity_UsesAttributeValue()
        {
            // Arrange & Act
            using var events = new SystemEvents<EventWithAttribute>();

            // Assert - The attribute specifies capacity 16
            // We can't directly test the capacity, but we verify it doesn't throw
            for (int i = 0; i < 20; i++)
            {
                events.Append(new EventWithAttribute { Id = i });
            }
            events.Tick(systemIndex: 0, currentFrame: 0);
            Assert.That(events.Count, Is.EqualTo(20));
        }

        #endregion

        #region Test Event Types

        private struct TestEvent
        {
            public int Value;
        }

        private struct AnotherTestEvent
        {
            public int Data;
        }

        [SystemEvent(InitCapacity = 16)]
        private struct EventWithAttribute
        {
            public int Id;
        }

        #endregion
    }
}
