#if ARCHETYPE_USE_UNITY_NATIVE_COLLECTION

using System;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine.TestTools;

namespace RelEcs.Tests
{
    public class NativeQueryTests
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
        public void EmptyQuery_ReturnsNoEntities()
        {
            // Create a query that matches no entities
            var mask = NativeBitArrayMask.Create();
            mask.Has(StorageType.Create<TestComponent>());

            var query = _archetypes.GetQuery(mask);

            int count = 0;
            foreach (var entity in query)
            {
                count++;
            }

            Assert.AreEqual(0, count, "Empty query should return no entities");
        }

        [Test]
        public void Query_MatchesEntitiesWithComponent()
        {
            // Create entities with different components
            var entity1 = _archetypes.Spawn();
            var entity2 = _archetypes.Spawn();
            var entity3 = _archetypes.Spawn();

            _archetypes.AddComponent(entity1.Identity, new TestComponent { Value = 1 });
            _archetypes.AddComponent(entity2.Identity, new TestComponent { Value = 2 });
            // entity3 doesn't have TestComponent

            // Create a query for entities with TestComponent
            var mask = NativeBitArrayMask.Create();
            mask.Has(StorageType.Create<TestComponent>());

            var query = _archetypes.GetQuery(mask);

            int count = 0;
            foreach (var entity in query)
            {
                count++;
                Assert.IsTrue(entity.Has<TestComponent>(), "Entity should have TestComponent");
            }

            Assert.AreEqual(2, count, "Query should match exactly 2 entities");
            Assert.IsTrue(query.Contains(entity1), "Query should contain entity1");
            Assert.IsTrue(query.Contains(entity2), "Query should contain entity2");
            Assert.IsFalse(query.Contains(entity3), "Query should not contain entity3");
        }

        [Test]
        public void Query_MatchesEntitiesWithMultipleComponents()
        {
            // Create entities with different components
            var entity1 = _archetypes.Spawn();
            var entity2 = _archetypes.Spawn();
            var entity3 = _archetypes.Spawn();

            _archetypes.AddComponent(entity1.Identity, new TestComponent { Value = 1 });
            _archetypes.AddComponent(entity1.Identity, new TestComponent2 { Value = 0.1f });

            _archetypes.AddComponent(entity2.Identity, new TestComponent { Value = 2 });
            // entity2 doesn't have TestComponent2

            _archetypes.AddComponent(entity3.Identity, new TestComponent2 { Value = 0.2f });
            // entity3 doesn't have TestComponent

            // Create a query for entities with both TestComponent and TestComponent2
            var mask = NativeBitArrayMask.Create();
            mask.Has(StorageType.Create<TestComponent>());
            mask.Has(StorageType.Create<TestComponent2>());

            var query = _archetypes.GetQuery(mask);

            int count = 0;
            foreach (var entity in query)
            {
                count++;
                Assert.IsTrue(entity.Has<TestComponent>(), "Entity should have TestComponent");
                Assert.IsTrue(entity.Has<TestComponent2>(), "Entity should have TestComponent2");
            }

            Assert.AreEqual(1, count, "Query should match exactly 1 entity");
            Assert.IsTrue(query.Contains(entity1), "Query should contain entity1");
            Assert.IsFalse(query.Contains(entity2), "Query should not contain entity2");
            Assert.IsFalse(query.Contains(entity3), "Query should not contain entity3");
        }

        [Test]
        public void Query_ExcludesComponents()
        {
            // Create entities with different components
            var entity1 = _archetypes.Spawn();
            var entity2 = _archetypes.Spawn();
            var entity3 = _archetypes.Spawn();

            _archetypes.AddComponent(entity1.Identity, new TestComponent { Value = 1 });

            _archetypes.AddComponent(entity2.Identity, new TestComponent { Value = 2 });
            _archetypes.AddComponent(entity2.Identity, new TestComponent2 { Value = 0.1f });

            _archetypes.AddComponent(entity3.Identity, new TestComponent { Value = 3 });

            // Create a query for entities with TestComponent but without TestComponent2
            var mask = NativeBitArrayMask.Create();
            mask.Has(StorageType.Create<TestComponent>());
            mask.Not(StorageType.Create<TestComponent2>());

            var query = _archetypes.GetQuery(mask);

            int count = 0;
            foreach (var entity in query)
            {
                count++;
                Assert.IsTrue(entity.Has<TestComponent>(), "Entity should have TestComponent");
                Assert.IsFalse(entity.Has<TestComponent2>(), "Entity should not have TestComponent2");
            }

            Assert.AreEqual(2, count, "Query should match exactly 2 entities");
            Assert.IsTrue(query.Contains(entity1), "Query should contain entity1");
            Assert.IsFalse(query.Contains(entity2), "Query should not contain entity2");
            Assert.IsTrue(query.Contains(entity3), "Query should contain entity3");
        }

        [Test]
        public void Query_AnyComponents()
        {
            // Create entities with different components
            var entity1 = _archetypes.Spawn();
            var entity2 = _archetypes.Spawn();
            var entity3 = _archetypes.Spawn();
            var entity4 = _archetypes.Spawn();

            _archetypes.AddComponent(entity1.Identity, new TestComponent { Value = 1 });

            _archetypes.AddComponent(entity2.Identity, new TestComponent2 { Value = 0.2f });

            _archetypes.AddComponent(entity3.Identity, new TestComponent { Value = 3 });
            _archetypes.AddComponent(entity3.Identity, new TestComponent2 { Value = 0.3f });

            // entity4 has neither component

            // Create a query for entities with either TestComponent or TestComponent2
            var mask = NativeBitArrayMask.Create();
            mask.Any(StorageType.Create<TestComponent>());
            mask.Any(StorageType.Create<TestComponent2>());

            var query = _archetypes.GetQuery(mask);

            int count = 0;
            foreach (var entity in query)
            {
                count++;
                Assert.IsTrue(entity.Has<TestComponent>() || entity.Has<TestComponent2>(),
                    "Entity should have either TestComponent or TestComponent2");
            }

            Assert.AreEqual(3, count, "Query should match exactly 3 entities");
            Assert.IsTrue(query.Contains(entity1), "Query should contain entity1");
            Assert.IsTrue(query.Contains(entity2), "Query should contain entity2");
            Assert.IsTrue(query.Contains(entity3), "Query should contain entity3");
            Assert.IsFalse(query.Contains(entity4), "Query should not contain entity4");
        }

        [Test]
        public void Query_ComponentAccess()
        {
            // Create an entity with components
            var entity = _archetypes.Spawn();
            _archetypes.AddComponent(entity.Identity, new TestComponent { Value = 42 });

            // Create a query
            var mask = NativeBitArrayMask.Create();
            mask.Has(StorageType.Create<TestComponent>());

            var query = _archetypes.GetQuery(mask);

            // Test component access through query
            Assert.IsTrue(query.Has<TestComponent>(entity), "Query should report entity has TestComponent");
            Assert.AreEqual(42, query.Get<TestComponent>(entity).Value, "Query should return correct component value");

            // Test component access through query entity
            var queryEntity = query.First();
            Assert.IsTrue(queryEntity.Has<TestComponent>(), "QueryEntity should report it has TestComponent");
            Assert.AreEqual(42, queryEntity.Get<TestComponent>().Value, "QueryEntity should return correct component value");

            // Test component modification
            ref var component = ref query.Get<TestComponent>(entity);
            component.Value = 100;

            Assert.AreEqual(100, query.Get<TestComponent>(entity).Value, "Component modification should be reflected");
        }

        [Test]
        public void Query_ForEach()
        {
            // Create entities
            var entity1 = _archetypes.Spawn();
            var entity2 = _archetypes.Spawn();

            _archetypes.AddComponent(entity1.Identity, new TestComponent { Value = 1 });
            _archetypes.AddComponent(entity2.Identity, new TestComponent { Value = 2 });

            // Create a query
            var mask = NativeBitArrayMask.Create();
            mask.Has(StorageType.Create<TestComponent>());

            var query = _archetypes.GetQuery(mask);

            // Test ForEach
            int sum = 0;
            query.ForEach(entity => {
                sum += query.Get<TestComponent>(entity).Value;
            });

            Assert.AreEqual(3, sum, "ForEach should process all matching entities");
        }

        [Test]
        public void Query_Single_WithOneMatch()
        {
            // Create one matching entity
            var entity = _archetypes.Spawn();
            _archetypes.AddComponent(entity.Identity, new TestComponent { Value = 42 });

            // Create a query
            var mask = NativeBitArrayMask.Create();
            mask.Has(StorageType.Create<TestComponent>());

            var query = _archetypes.GetQuery(mask);

            // Test Single
            var result = query.Single();
            Assert.AreEqual(entity.Identity, result.Entity.Identity, "Single should return the matching entity");
        }

        [Test]
        public void Query_Single_WithNoMatches_ThrowsException()
        {
            // Create a query with no matches
            var mask = NativeBitArrayMask.Create();
            mask.Has(StorageType.Create<TestComponent>());

            var query = _archetypes.GetQuery(mask);

            // Test Single throws exception
            Assert.Throws<NoElementsException>(() => query.Single());
        }

        [Test]
        public void Query_Single_WithMultipleMatches_ThrowsException()
        {
            // Create multiple matching entities
            var entity1 = _archetypes.Spawn();
            var entity2 = _archetypes.Spawn();

            _archetypes.AddComponent(entity1.Identity, new TestComponent { Value = 1 });
            _archetypes.AddComponent(entity2.Identity, new TestComponent { Value = 2 });

            // Create a query
            var mask = NativeBitArrayMask.Create();
            mask.Has(StorageType.Create<TestComponent>());

            var query = _archetypes.GetQuery(mask);

            // Test Single throws exception
            Assert.Throws<MoreThanOneElementsException>(() => query.Single());
        }

        [Test]
        public void Query_SingleOrDefault_WithOneMatch()
        {
            // Create one matching entity
            var entity = _archetypes.Spawn();
            _archetypes.AddComponent(entity.Identity, new TestComponent { Value = 42 });

            // Create a query
            var mask = NativeBitArrayMask.Create();
            mask.Has(StorageType.Create<TestComponent>());

            var query = _archetypes.GetQuery(mask);

            // Test SingleOrDefault
            var result = query.SingleOrDefault();
            Assert.AreEqual(entity.Identity, result.Entity.Identity, "SingleOrDefault should return the matching entity");
        }

        [Test]
        public void Query_SingleOrDefault_WithNoMatches_ReturnsDefault()
        {
            // Create a query with no matches
            var mask = NativeBitArrayMask.Create();
            mask.Has(StorageType.Create<TestComponent>());

            var query = _archetypes.GetQuery(mask);

            // Test SingleOrDefault returns default
            var result = query.SingleOrDefault();
            Assert.AreEqual(Entity.None.Identity, result.Entity.Identity, "SingleOrDefault should return Entity.None");
        }

        [Test]
        public void Query_First_WithMatches()
        {
            // Create multiple matching entities
            var entity1 = _archetypes.Spawn();
            var entity2 = _archetypes.Spawn();

            _archetypes.AddComponent(entity1.Identity, new TestComponent { Value = 1 });
            _archetypes.AddComponent(entity2.Identity, new TestComponent { Value = 2 });

            // Create a query
            var mask = NativeBitArrayMask.Create();
            mask.Has(StorageType.Create<TestComponent>());

            var query = _archetypes.GetQuery(mask);

            // Test First
            var result = query.First();
            Assert.IsTrue(result.Entity.Identity.Equals(entity1.Identity) || result.Entity.Identity.Equals(entity2.Identity),
                "First should return one of the matching entities");
        }

        [Test]
        public void Query_First_WithNoMatches_ThrowsException()
        {
            // Create a query with no matches
            var mask = NativeBitArrayMask.Create();
            mask.Has(StorageType.Create<TestComponent>());

            var query = _archetypes.GetQuery(mask);

            // Test First throws exception
            Assert.Throws<NoElementsException>(() => query.First());
        }

        [Test]
        public void Query_DynamicallyUpdates_WhenEntitiesChange()
        {
            // Create a query first
            var mask = NativeBitArrayMask.Create();
            mask.Has(StorageType.Create<TestComponent>());

            var query = _archetypes.GetQuery(mask);

            // Initially no entities match
            Assert.AreEqual(0, CountEntities(query), "Query should initially match 0 entities");

            // Add an entity that matches
            var entity = _archetypes.Spawn();
            _archetypes.AddComponent(entity.Identity, new TestComponent { Value = 42 });

            // Query should update automatically
            Assert.AreEqual(1, CountEntities(query), "Query should update to match 1 entity");

            // Remove the component
            _archetypes.RemoveComponent<TestComponent>(entity.Identity);

            // Query should update again
            Assert.AreEqual(0, CountEntities(query), "Query should update to match 0 entities again");
        }

        private int CountEntities(NativeQuery query)
        {
            int count = 0;
            foreach (var entity in query)
            {
                count++;
            }
            return count;
        }

        // Test component types
        private struct TestComponent
        {
            public int Value;
        }

        private struct TestComponent2
        {
            public float Value;
        }
    }
}

#endif
