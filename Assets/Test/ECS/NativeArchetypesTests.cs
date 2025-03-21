#if ARCHETYPE_USE_UNITY_NATIVE_COLLECTION

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace RelEcs.Tests
{
#pragma warning disable CS0169
    [TestFixture]
    public class NativeArchetypesTests
    {
        private NativeArchetypes _archetypes;

        [SetUp]
        public void Setup()
        {
            _archetypes = new NativeArchetypes(Allocator.Persistent);
        }

        [TearDown]
        public void TearDown()
        {
            _archetypes.Dispose();
        }

        [Test]
        public void Constructor_InitializesProperly()
        {
            Assert.DoesNotThrow(() => new NativeArchetypes(Allocator.Temp).Dispose());
        }

        [Test]
        public void Spawn_ReturnsValidEntity()
        {
            var entity = _archetypes.Spawn();
            Assert.That(_archetypes.IsAlive(entity.Identity), Is.True);
        }

        [Test]
        public void IsAlive_Generation()
        {
            var entity = _archetypes.Spawn();
            Assert.That(_archetypes.IsAlive(entity.Identity), Is.True);

            _archetypes.Despawn(entity.Identity);
            Assert.That(_archetypes.IsAlive(entity.Identity), Is.False);

            var entity2 = _archetypes.Spawn();
            Assert.That(entity.Identity.Index, Is.EqualTo(entity2.Identity.Index));
            Assert.That(entity.Identity.Generation + 1, Is.EqualTo(entity2.Identity.Generation));
            Assert.That(_archetypes.IsAlive(entity.Identity), Is.False);
            Assert.That(_archetypes.IsAlive(entity2.Identity), Is.True);
        }

        [Test]
        public void Despawn_RemovesEntity()
        {
            var entity = _archetypes.Spawn();
            _archetypes.Despawn(entity.Identity);
            Assert.That(_archetypes.IsAlive(entity.Identity), Is.False);
        }

        [Test]
        public void Despawn_DoesNothing_WhenEntityIsNotAlive()
        {
            var entity = new Entity(new Identity(999, 1)); // Assuming this ID is not used
            Assert.DoesNotThrow(() => _archetypes.Despawn(entity.Identity));
        }

        [Test]
        public void Despawn_RemovesEntityAndItsComponents()
        {
            // Arrange
            var entity = _archetypes.Spawn();
            _archetypes.AddComponent(entity.Identity, 42);

            // Act
            _archetypes.Despawn(entity.Identity);

            // Assert
            Assert.That(_archetypes.IsAlive(entity.Identity), Is.False);
            Assert.Throws<ArgumentException>(() => _archetypes.HasComponent<int>(entity.Identity));
        }

        [Test]
        public void Despawn_DoesNotRemoveOtherEntities()
        {
            // Arrange
            var entity1 = _archetypes.Spawn();
            var entity2 = _archetypes.Spawn();

            // Act
            _archetypes.Despawn(entity1.Identity);

            // Assert
            Assert.That(_archetypes.IsAlive(entity1.Identity), Is.False);
            Assert.That(_archetypes.IsAlive(entity2.Identity), Is.True);
        }

        [Test]
        public void AddComponent_AddsComponentToEntity()
        {
            var entity = _archetypes.Spawn();
            _archetypes.AddComponent(entity.Identity, 123);
            Assert.That(_archetypes.HasComponent<int>(entity.Identity), Is.True);
        }

        [Test]
        public void GetComponent_ReturnsValidComponent()
        {
            var entity = _archetypes.Spawn();
            var data = 123;
            _archetypes.AddComponent(entity.Identity, data);
            var component = _archetypes.GetComponent<int>(entity.Identity);
            Assert.That(component, Is.EqualTo(data));
        }

        [Test]
        public void AddComponent_OverwritesWhenComponentAlreadyExists()
        {
            var entity = _archetypes.Spawn();
            _archetypes.AddComponent(entity.Identity, 123);
            Assert.That(_archetypes.GetComponent<int>(entity.Identity), Is.EqualTo(123));

            _archetypes.AddComponent(entity.Identity, 321);
            Assert.That(_archetypes.GetComponent<int>(entity.Identity), Is.EqualTo(321));
        }

        [Test]
        public void RemoveComponent_ThrowsWhenComponentDoesNotExist()
        {
            var entity = _archetypes.Spawn();
            Assert.Throws<Exception>(() => _archetypes.RemoveComponent<float>(entity.Identity));
        }

        [Test]
        public void GetComponent_ThrowsWhenComponentDoesNotExist()
        {
            var entity = _archetypes.Spawn();
            Assert.Catch<Exception>(() => _archetypes.GetComponent<int>(entity.Identity));
        }

        [Test]
        public void Spawn_WhenEntityLimitReached_ResizesMetaArray()
        {
            // Create many entities to test resizing
            var entities = new List<Entity>();
            for (int i = 0; i < 600; i++) // Initial capacity is 512
            {
                entities.Add(_archetypes.Spawn());
            }

            // Verify all entities are alive
            foreach (var entity in entities)
            {
                Assert.That(_archetypes.IsAlive(entity.Identity), Is.True);
            }
        }

        [Test]
        public void GetComponent_ReturnsCorrectComponent()
        {
            var entity = _archetypes.Spawn();
            var data = 123;
            _archetypes.AddComponent(entity.Identity, data);
            var component = _archetypes.GetComponent<int>(entity.Identity);
            Assert.That(component, Is.EqualTo(data));
        }

        [Test]
        public void GetComponent_ThrowsWhenEntityDoesNotExist()
        {
            var nonExistentIdentity = new Identity(999, 1);
            Assert.Throws<ArgumentException>(() => _archetypes.GetComponent<int>(nonExistentIdentity));
        }

        [Test]
        public void MultipleComponentsWithSameType_Overwrite()
        {
            var entity = _archetypes.Spawn();
            _archetypes.AddComponent(entity.Identity, 1);
            Assert.That(_archetypes.GetComponent<int>(entity.Identity), Is.EqualTo(1));

            _archetypes.AddComponent(entity.Identity, 2);
            Assert.That(_archetypes.GetComponent<int>(entity.Identity), Is.EqualTo(2));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TestRawData
        {
            public int Value1;
            public float Value2;
            public byte Value3;
            public bool Value4;
        }

        [Test]
        public unsafe void GetComponent_WithComplexStruct_PreservesAllFields()
        {
            // Arrange
            var entity = _archetypes.Spawn();
            var testData = new TestRawData { Value1 = 42, Value2 = 3.14f, Value3 = 255, Value4 = true };

            // Act
            _archetypes.AddComponent(entity.Identity, testData);

            // Assert
            var retrievedData = _archetypes.GetComponent<TestRawData>(entity.Identity);
            Assert.That(retrievedData.Value1, Is.EqualTo(testData.Value1));
            Assert.That(retrievedData.Value2, Is.EqualTo(testData.Value2));
            Assert.That(retrievedData.Value3, Is.EqualTo(testData.Value3));
            Assert.That(retrievedData.Value4, Is.EqualTo(testData.Value4));
        }

        [Test]
        public void AddMultipleComponentTypes_CreatesCorrectTable()
        {
            // Arrange
            var entity = _archetypes.Spawn();

            // Act
            _archetypes.AddComponent(entity.Identity, 42);
            _archetypes.AddComponent(entity.Identity, 3.14f);
            _archetypes.AddComponent(entity.Identity, new Vector3(1, 2, 3));

            // Assert
            Assert.That(_archetypes.HasComponent<int>(entity.Identity), Is.True);
            Assert.That(_archetypes.HasComponent<float>(entity.Identity), Is.True);
            Assert.That(_archetypes.HasComponent<Vector3>(entity.Identity), Is.True);

            Assert.That(_archetypes.GetComponent<int>(entity.Identity), Is.EqualTo(42));
            Assert.That(_archetypes.GetComponent<float>(entity.Identity), Is.EqualTo(3.14f));
            Assert.That(_archetypes.GetComponent<Vector3>(entity.Identity), Is.EqualTo(new Vector3(1, 2, 3)));
        }

        [Test]
        public void RemoveComponent_RemovesComponentFromEntity()
        {
            // Arrange
            var entity = _archetypes.Spawn();
            _archetypes.AddComponent(entity.Identity, 42);
            _archetypes.AddComponent(entity.Identity, 3.14f);

            // Act
            _archetypes.RemoveComponent<int>(entity.Identity);

            // Assert
            Assert.That(_archetypes.HasComponent<int>(entity.Identity), Is.False);
            Assert.That(_archetypes.HasComponent<float>(entity.Identity), Is.True);
        }

        [Test]
        public void EntityReuse_PreservesGenerationCounting()
        {
            // Arrange
            var entities = new List<Entity>();
            for (int i = 0; i < 10; i++)
            {
                entities.Add(_archetypes.Spawn());
            }

            // Act - despawn all entities
            foreach (var entity in entities)
            {
                _archetypes.Despawn(entity.Identity);
            }

            // Spawn new entities - should reuse indices with new generations
            var newEntities = new List<Entity>();
            for (int i = 0; i < 10; i++)
            {
                newEntities.Add(_archetypes.Spawn());
            }

            // Assert
            for (int i = 0; i < 10; i++)
            {
                Assert.That(newEntities[i].Identity.Index, Is.EqualTo(entities[i].Identity.Index));
                Assert.That(newEntities[i].Identity.Generation, Is.EqualTo(entities[i].Identity.Generation + 1));
                Assert.That(_archetypes.IsAlive(entities[i].Identity), Is.False);
                Assert.That(_archetypes.IsAlive(newEntities[i].Identity), Is.True);
            }
        }

        [Test]
        public void AddRemoveComponent_MaintainsTableStructure()
        {
            // Arrange
            var entity = _archetypes.Spawn();

            // Act & Assert - Add components
            _archetypes.AddComponent(entity.Identity, 42);
            Assert.That(_archetypes.HasComponent<int>(entity.Identity), Is.True);

            _archetypes.AddComponent(entity.Identity, 3.14f);
            Assert.That(_archetypes.HasComponent<int>(entity.Identity), Is.True);
            Assert.That(_archetypes.HasComponent<float>(entity.Identity), Is.True);

            // Remove components
            _archetypes.RemoveComponent<float>(entity.Identity);
            Assert.That(_archetypes.HasComponent<int>(entity.Identity), Is.True);
            Assert.That(_archetypes.HasComponent<float>(entity.Identity), Is.False);

            _archetypes.RemoveComponent<int>(entity.Identity);
            Assert.That(_archetypes.HasComponent<int>(entity.Identity), Is.False);
        }

        [Test]
        public void MultipleEntitiesWithSameComponents_ShareTable()
        {
            // This test verifies that entities with the same component types share tables
            // We can't directly test this with the public API, but we can test the behavior

            // Arrange
            var entity1 = _archetypes.Spawn();
            _archetypes.AddComponent(entity1.Identity, 1);
            _archetypes.AddComponent(entity1.Identity, 1.0f);

            var entity2 = _archetypes.Spawn();
            _archetypes.AddComponent(entity2.Identity, 2);
            _archetypes.AddComponent(entity2.Identity, 2.0f);

            // Act - modify one entity's component
            _archetypes.GetComponent<int>(entity1.Identity) = 3;

            // Assert - only that entity should be affected
            Assert.That(_archetypes.GetComponent<int>(entity1.Identity), Is.EqualTo(3));
            Assert.That(_archetypes.GetComponent<int>(entity2.Identity), Is.EqualTo(2));
        }

        [Test]
        public void DisposeTest_ReleasesAllResources()
        {
            // Arrange
            var tempArchetypes = new NativeArchetypes(Allocator.TempJob);
            var entity = tempArchetypes.Spawn();
            tempArchetypes.AddComponent(entity.Identity, 42);

            // Act
            tempArchetypes.Dispose();

            // Assert - can't directly test memory release, but we can check for exceptions
            Assert.Throws<ObjectDisposedException>(() => tempArchetypes.Spawn());
        }

        // [Test, Ignore("jobs isn't supported yet")]
        // public void AsyncDisposeTest()
        // {
        //     // Arrange
        //     var tempArchetypes = new NativeArchetypes(Allocator.TempJob);
        //     var entity = tempArchetypes.Spawn();
        //     tempArchetypes.AddComponent(entity.Identity, 42);
        //
        //     // Act
        //     var jobHandle = tempArchetypes.Dispose(default);
        //
        //     // Complete the job
        //     jobHandle.Complete();
        //
        //     // Assert - can't directly test memory release, but we can check for exceptions
        //     Assert.Throws<ObjectDisposedException>(() => tempArchetypes.Spawn());
        // }

        [StructLayout(LayoutKind.Sequential, Size = 0)]
        private struct ZeroSizeTag { }

        [Test]
        public void TagComponents_AreHandledCorrectly()
        {
            // Arrange
            var entity = _archetypes.Spawn();

            // Act
            _archetypes.AddComponent(entity.Identity, new ZeroSizeTag());

            // Assert
            Assert.That(_archetypes.HasComponent<ZeroSizeTag>(entity.Identity), Is.True);
        }

        [Test]
        public void MultipleEntities_WithDifferentComponents()
        {
            // Arrange
            var entities = new List<Entity>();
            for (int i = 0; i < 100; i++)
            {
                var entity = _archetypes.Spawn();
                entities.Add(entity);

                // Add different combinations of components
                if (i % 2 == 0) _archetypes.AddComponent(entity.Identity, i);
                if (i % 3 == 0) _archetypes.AddComponent(entity.Identity, (float)i);
                if (i % 5 == 0) _archetypes.AddComponent(entity.Identity, new Vector3(i, i, i));
            }

            // Assert
            for (int i = 0; i < 100; i++)
            {
                if (i % 2 == 0) Assert.That(_archetypes.HasComponent<int>(entities[i].Identity), Is.True);
                else Assert.That(_archetypes.HasComponent<int>(entities[i].Identity), Is.False);

                if (i % 3 == 0) Assert.That(_archetypes.HasComponent<float>(entities[i].Identity), Is.True);
                else Assert.That(_archetypes.HasComponent<float>(entities[i].Identity), Is.False);

                if (i % 5 == 0) Assert.That(_archetypes.HasComponent<Vector3>(entities[i].Identity), Is.True);
                else Assert.That(_archetypes.HasComponent<Vector3>(entities[i].Identity), Is.False);
            }
        }

        [Test]
        public void StressTest_ManyEntitiesAndComponents()
        {
            // This test creates many entities with various components to stress the system
            const int entityCount = 1000;

            // Arrange & Act
            var entities = new NativeArray<Entity>(entityCount, Allocator.Temp);

            for (int i = 0; i < entityCount; i++)
            {
                entities[i] = _archetypes.Spawn();

                // Add components based on index patterns
                if (i % 2 == 0) _archetypes.AddComponent(entities[i].Identity, i);
                if (i % 3 == 0) _archetypes.AddComponent(entities[i].Identity, (float)i);
                if (i % 5 == 0) _archetypes.AddComponent(entities[i].Identity, new Vector3(i, i, i));
            }

            // Despawn some entities
            for (int i = 0; i < entityCount; i += 7)
            {
                _archetypes.Despawn(entities[i].Identity);
            }

            // Spawn some new entities
            for (int i = 0; i < entityCount / 10; i++)
            {
                var entity = _archetypes.Spawn();
                _archetypes.AddComponent(entity.Identity, i * 1000);
            }

            // Assert - just check that we don't throw exceptions
            // The test passes if it completes without errors

            entities.Dispose();
        }

        [Test, Ignore("parallel isn't supported yet")]
        public void ParallelEntityCreation()
        {
            // This test verifies that we can create entities from multiple threads
            const int entityCount = 1000;

            // Arrange
            var entities = new NativeArray<Entity>(entityCount, Allocator.TempJob);

            // Act
            var job = new SpawnEntitiesJob
            {
                Entities = entities
            };

            job.Run(); // Run directly for simplicity

            // Assert
            for (int i = 0; i < entityCount; i++)
            {
                Assert.That(entities[i].Identity.Index, Is.GreaterThanOrEqualTo(0));
                Assert.That(_archetypes.IsAlive(entities[i].Identity), Is.True);
            }

            entities.Dispose();
        }

        private struct SpawnEntitiesJob : IJob
        {
            public NativeArray<Entity> Entities;

            public void Execute()
            {
                var archetypes = new NativeArchetypes(Allocator.Temp);

                for (int i = 0; i < Entities.Length; i++)
                {
                    Entities[i] = archetypes.Spawn();
                    if (i % 2 == 0) archetypes.AddComponent(Entities[i].Identity, i);
                    if (i % 3 == 0) archetypes.AddComponent(Entities[i].Identity, (float)i);
                }

                archetypes.Dispose();
            }
        }

        [Test]
        public void ComponentTypeHashCollision_HandledCorrectly()
        {
            // This test verifies that the system handles hash collisions correctly
            // We can't directly force a collision, but we can test with many different component types

            // Arrange
            var entity = _archetypes.Spawn();

            // Act - add many different component types
            _archetypes.AddComponent(entity.Identity, (byte)1);
            _archetypes.AddComponent(entity.Identity, (short)2);
            _archetypes.AddComponent(entity.Identity, (int)3);
            _archetypes.AddComponent(entity.Identity, (long)4);
            _archetypes.AddComponent(entity.Identity, (float)5);
            _archetypes.AddComponent(entity.Identity, (double)6);
            _archetypes.AddComponent(entity.Identity, new Vector2(7, 7));
            _archetypes.AddComponent(entity.Identity, new Vector3(8, 8, 8));
            _archetypes.AddComponent(entity.Identity, new Vector4(9, 9, 9, 9));
            _archetypes.AddComponent(entity.Identity, new Quaternion(10, 10, 10, 10));

            // Assert - all components should be retrievable
            Assert.That(_archetypes.GetComponent<byte>(entity.Identity), Is.EqualTo((byte)1));
            Assert.That(_archetypes.GetComponent<short>(entity.Identity), Is.EqualTo((short)2));
            Assert.That(_archetypes.GetComponent<int>(entity.Identity), Is.EqualTo(3));
            Assert.That(_archetypes.GetComponent<long>(entity.Identity), Is.EqualTo(4));
            Assert.That(_archetypes.GetComponent<float>(entity.Identity), Is.EqualTo(5));
            Assert.That(_archetypes.GetComponent<double>(entity.Identity), Is.EqualTo(6));
            Assert.That(_archetypes.GetComponent<Vector2>(entity.Identity), Is.EqualTo(new Vector2(7, 7)));
            Assert.That(_archetypes.GetComponent<Vector3>(entity.Identity), Is.EqualTo(new Vector3(8, 8, 8)));
            Assert.That(_archetypes.GetComponent<Vector4>(entity.Identity), Is.EqualTo(new Vector4(9, 9, 9, 9)));
            Assert.That(_archetypes.GetComponent<Quaternion>(entity.Identity), Is.EqualTo(new Quaternion(10, 10, 10, 10)));
        }

        [Test]
        public void AllocatorTypes_AreRespected()
        {
            // Test with different allocator types
            using (var tempArchetypes = new NativeArchetypes(Allocator.Temp))
            {
                var entity = tempArchetypes.Spawn();
                Assert.That(tempArchetypes.IsAlive(entity.Identity), Is.True);
            }

            using (var tempJobArchetypes = new NativeArchetypes(Allocator.TempJob))
            {
                var entity = tempJobArchetypes.Spawn();
                Assert.That(tempJobArchetypes.IsAlive(entity.Identity), Is.True);
            }

            using (var persistentArchetypes = new NativeArchetypes(Allocator.Persistent))
            {
                var entity = persistentArchetypes.Spawn();
                Assert.That(persistentArchetypes.IsAlive(entity.Identity), Is.True);
            }
        }
    }
}

#endif
