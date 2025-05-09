using System;
using NUnit.Framework;

namespace Overlook.Ecs.Tests
{
    [TestFixture]
    public class PoolEnhancementsTests
    {
        private World _world;

        [SetUp]
        public void Setup()
        {
            _world = new World();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }
        
        // This test verifies that IsAlive handles out-of-bounds indices properly
        [Test]
        public void IsAlive_WithOutOfBoundsIndex_ReturnsFalse()
        {
            // Create an identity with a large index
            var largeIndex = 9999;
            var identity = new Identity(largeIndex, 1);
            var entity = new Entity(identity);
            
            // Should safely return false without exceptions
            Assert.That(_world.IsAlive(entity), Is.False);
        }
        
        // Test the capacity management for large indices
        [Test]
        public void Pool_WithLargeIndex_ExpandsCapacity()
        {
            // Create an identity with a large index
            var largeIndex = 1000; // Should be larger than the initial capacity
            var identity = new Identity(largeIndex, 1);
            var entity = new Entity(identity);
            
            // Use this entity (will cause Pool to expand)
            _world.Use(entity);
            
            // Entity should be alive
            Assert.That(_world.IsAlive(entity), Is.True);
            
            // We should be able to add components to it
            _world.AddComponent(entity, new Position(123, 456));
            var pos = _world.GetComponent<Position>(entity);
            Assert.That(pos.X, Is.EqualTo(123));
        }
        
        // Test the Use method with a specific identity
        [Test]
        public void Use_CreatesEntityWithSpecificIdentity()
        {
            // Create a specific identity
            var identity = new Identity(42, 7);
            var entity = new Entity(identity);
            
            // Use it
            _world.Use(entity);
            
            // The entity should be alive and have the exact identity we specified
            Assert.That(_world.IsAlive(entity), Is.True);
            
            // Verify the identity is preserved by spawning a new entity and comparing
            var newEntity = _world.Spawn();
            Assert.That(newEntity.Identity.Index, Is.Not.EqualTo(entity.Identity.Index));
            Assert.That(newEntity.Identity.Generation, Is.Not.EqualTo(entity.Identity.Generation));
        }
        
        // Test that large skips in index don't waste memory
        [Test]
        public void Use_WithLargeGaps_HandlesEfficientlyWithoutOOM()
        {
            // We'll create several entities with very large indices
            // This shouldn't cause excessive memory usage
            
            for (int i = 0; i < 5; i++)
            {
                // Each entity has a progressively larger index
                var index = 1000 * (i + 1);
                var identity = new Identity(index, 1);
                var entity = new Entity(identity);
                
                // Should not throw OOM
                Assert.DoesNotThrow(() => _world.Use(entity));
                
                // Entity should be alive
                Assert.That(_world.IsAlive(entity), Is.True);
                
                // Add a component to verify it's functional
                _world.AddComponent(entity, new Position(i, i*2));
            }
            
            // Verify we can retrieve each entity's components
            for (int i = 0; i < 5; i++)
            {
                var index = 1000 * (i + 1);
                var identity = new Identity(index, 1);
                var entity = new Entity(identity);
                
                Assert.That(_world.IsAlive(entity), Is.True);
                var pos = _world.GetComponent<Position>(entity);
                Assert.That(pos.X, Is.EqualTo(i));
                Assert.That(pos.Y, Is.EqualTo(i*2));
            }
        }
        
        // Test the improved entity removal and recycling
        [Test]
        public void Despawn_RecyclesEntityIdsCorrectly()
        {
            // Create several entities
            var entities = new Entity[10];
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i] = _world.Spawn();
                _world.AddComponent(entities[i], new Position(i, i*2));
            }
            
            // Despawn a few of them
            _world.Despawn(entities[2]);
            _world.Despawn(entities[5]);
            _world.Despawn(entities[8]);
            
            // Verify they're no longer alive
            Assert.That(_world.IsAlive(entities[2]), Is.False);
            Assert.That(_world.IsAlive(entities[5]), Is.False);
            Assert.That(_world.IsAlive(entities[8]), Is.False);
            
            // Spawn new entities, which should reuse the released IDs
            var newEntities = new Entity[3];
            for (int i = 0; i < newEntities.Length; i++)
            {
                newEntities[i] = _world.Spawn();
            }
            
            // At least one of the new entities should reuse a previous index
            // Note: We can't guarantee the exact order of recycling since the implementation
            // changed from Queue to List
            var recycledIndices = new[] { 
                entities[2].Identity.Index, 
                entities[5].Identity.Index, 
                entities[8].Identity.Index 
            };
            
            bool foundRecycled = false;
            foreach (var entity in newEntities)
            {
                foreach (var index in recycledIndices)
                {
                    if (entity.Identity.Index == index)
                    {
                        foundRecycled = true;
                        // Generation should be incremented
                        Assert.That(entity.Identity.Generation, Is.GreaterThan(1));
                        break;
                    }
                }
            }
            
            Assert.That(foundRecycled, Is.True, "None of the new entities reused a recycled index");
        }
    }
} 