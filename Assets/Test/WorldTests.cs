#nullable disable

using System;
using System.Linq;
using NUnit.Framework;

namespace RelEcs.Tests
{
    public class Position
    {
        public int X { get; }
        public int Y { get; }
        public Position(int x, int y) { X = x; Y = y; }
        public Position() {}
    }

    public class Velocity
    {
        public int X { get; }
        public int Y { get; }
        public Velocity(int x, int y) { X = x; Y = y; }
        public Velocity() {}
    }

    public class SomeNonExistentComponent { }
    public class SomeElement { }

    public class Health
    {
        public int Value;
        public Health(int value) { Value = value; }
        public Health() { }
    }

    [TestFixture]
    public class WorldTests
    {
        private World _world;

        [SetUp]
        public void Setup()
        {
            _world = new World();
        }

        [Test]
        public void Query_ReturnsQueryBuilder()
        {
            _world.Spawn().Add<Position>();
            _world.Spawn().Add<Position>().Add<Velocity>();
            _world.Spawn().Add<Health>();
            var queryBuilder = _world.Query<Position>();
            Assert.IsInstanceOf<QueryBuilder>(queryBuilder);
        }

        [Test]
        public void Query_WithComponent_ReturnsCorrectEntities()
        {
            _world.Spawn().Add<Position>();
            _world.Spawn().Add<Position>().Add<Velocity>();
            _world.Spawn().Add<Health>();
            var query = _world.Query<Position>().Build();
            var count = query.GetEnumerator().Count();
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void Query_WithoutComponent_ReturnsCorrectEntities()
        {
            _world.Spawn().Add<Position>();
            _world.Spawn().Add<Position>().Add<Velocity>();
            _world.Spawn().Add<Health>();
            var query = _world.Query<Position>().Not<Velocity>().Build();
            var count = query.GetEnumerator().Count();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Query_WithMultipleComponents_ReturnsCorrectEntities()
        {
            _world.Spawn().Add<Position>();
            _world.Spawn().Add<Position>().Add<Velocity>();
            _world.Spawn().Add<Health>();
            var query = _world.Query<Position>().Has<Velocity>().Build();
            var count = query.GetEnumerator().Count();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Query_WithoutMultipleComponents_ReturnsCorrectEntities()
        {
            _world.Spawn().Add<Position>();
            _world.Spawn().Add<Position>().Add<Velocity>();
            _world.Spawn().Add<Health>();
            var query = _world.Query<Position>().Not<Velocity>().Not<Health>().Build();
            var count = query.GetEnumerator().Count();
            Assert.That(count, Is.EqualTo(1));
        }


        [Test]
        public void Despawn_Entity_SuccessfullyDespawned()
        {
            var entity = _world.Spawn().Add<Position>().Add<Velocity>().Id();
            _world.Despawn(entity);
            Assert.That(_world.IsAlive(entity), Is.False);
        }

        [Test]
        public void Despawn_NonExistentEntity_Thrown()
        {
            var entity = new Entity(new Identity(999)); // Assuming 999 is an invalid ID
            Assert.Catch<Exception>(() => _world.Despawn(entity));
        }

        [Test]
        public void Despawn_MultipleTimes_NoExceptionThrown()
        {
            var entity = _world.Spawn().Add<Position>().Add<Velocity>().Id();
            _world.Despawn(entity);
            Assert.DoesNotThrow(() => _world.Despawn(entity));
        }

        [Test]
        public void Despawn_AfterAddingComponents_ComponentsRemoved()
        {
            // Assuming we have a component called Position
            var entity = _world.Spawn().Add<Position>().Id();
            _world.Despawn(entity);
            Assert.That(_world.HasComponent<Position>(entity), Is.False);
        }

        [Test]
        public void DespawnAllWith_Position()
        {
            // Assuming we have a component called Position
            var entity1 = _world.Spawn().Add<Position>().Id();
            var entity2 = _world.Spawn().Add<Position>().Add<Velocity>().Id();
            _world.DespawnAllWith<Position>();
            Assert.That(_world.IsAlive(entity1), Is.False);
            Assert.That(_world.IsAlive(entity2), Is.False);
        }

        [Test]
        public void GetComponent_EntityWithComponent_SuccessfullyRetrieved()
        {
            var entity = _world.Spawn().Id();
            var position = new Position();
            _world.AddComponent(entity, position);
            var retrievedPosition = _world.GetComponent<Position>(entity);
            Assert.That(retrievedPosition, Is.SameAs(position));
        }

        [Test]
        public void GetComponent_NonExistentEntity_Thrown()
        {
            var entity = new Entity(new Identity(999)); // Assuming 999 is an invalid ID
            Assert.Catch<Exception>(() => _world.GetComponent<Position>(entity));
        }

        [Test]
        public void GetComponent_EntityWithoutComponent_Thrown()
        {
            var entity = _world.Spawn().Id();
            Assert.Catch<Exception>(() => _world.GetComponent<Position>(entity));
        }

        [Test]
        public void GetComponent_AfterRemovingComponent_Thrown()
        {
            var entity = _world.Spawn().Id();
            var position = new Position();
            _world.AddComponent(entity, position);
            _world.RemoveComponent<Position>(entity);
            Assert.Catch<Exception>(() => _world.GetComponent<Position>(entity));
        }


        [Test]
        public void GetComponent_WithTarget_SuccessfullyRetrieved()
        {
            var entity = _world.Spawn().Id();
            var targetEntity = _world.Spawn().Id();
            var position = new Position();
            _world.AddComponent(entity, position, targetEntity);
            var retrievedPosition = _world.GetComponent<Position>(entity, targetEntity);
            Assert.That(retrievedPosition, Is.SameAs(position));
        }

        [Test]
        public void GetComponent_NonExistentEntityWithTarget_Thrown()
        {
            var entity = new Entity(new Identity(999)); // Assuming 999 is an invalid ID
            var targetEntity = _world.Spawn().Id();
            Assert.Catch<Exception>(() => _world.GetComponent<Position>(entity, targetEntity));
        }

        [Test]
        public void GetComponent_EntityWithoutComponentWithTarget_Thrown()
        {
            var entity = _world.Spawn().Id();
            var targetEntity = _world.Spawn().Id();
            Assert.Catch<Exception>(() => _world.GetComponent<Position>(entity, targetEntity));
        }

        [Test]
        public void GetComponent_WithInvalidTarget_ReturnsNull()
        {
            var entity = _world.Spawn().Id();
            var invalidTarget = new Entity(new Identity(888)); // Assuming 888 is an invalid ID
            Assert.Catch<Exception>(() => _world.GetComponent<Position>(entity, invalidTarget));
        }

        [Test]
        public void GetComponent_AfterRemovingComponentWithTarget_Thrown()
        {
            var entity = _world.Spawn().Id();
            var targetEntity = _world.Spawn().Id();
            var position = new Position();
            _world.AddComponent(entity, position, targetEntity);
            _world.RemoveComponent<Position>(entity, targetEntity);
            Assert.Catch<Exception>(() => _world.GetComponent<Position>(entity, targetEntity));
        }

        [Test]
        public void TryGetComponent_SuccessfullyRetrieved()
        {
            var entity = _world.Spawn().Id();
            var position = new Position();
            _world.AddComponent(entity, position);
            Assert.That(_world.TryGetComponent(entity, out Position retrievedPosition), Is.True);
            Assert.That(retrievedPosition, Is.SameAs(position));
        }

        [Test]
        public void TryGetComponent_NonExistentEntity_ReturnsFalse()
        {
            var entity = new Entity(new Identity(999)); // Assuming 999 is an invalid ID
            Assert.Catch<Exception>(() => _world.TryGetComponent(entity, out Position _));
        }

        [Test]
        public void TryGetComponent_EntityWithoutComponent_ReturnsFalse()
        {
            var entity = _world.Spawn().Id();
            Assert.That(_world.TryGetComponent(entity, out Position _), Is.False);
        }

        [Test]
        public void TryGetComponent_WithTarget_SuccessfullyRetrieved()
        {
            var entity = _world.Spawn().Id();
            var targetEntity = _world.Spawn().Id();
            var position = new Position();
            _world.AddComponent(entity, position, targetEntity);
            Assert.That(_world.TryGetComponent(entity, out Position retrievedPosition, targetEntity), Is.True);
            Assert.That(retrievedPosition, Is.SameAs(position));
        }

        [Test]
        public void TryGetComponent_NonExistentEntityWithTarget_ReturnsFalse()
        {
            var entity = new Entity(new Identity(999)); // Assuming 999 is an invalid ID
            var targetEntity = _world.Spawn().Id();
            Assert.Catch<Exception>(() => _world.TryGetComponent(entity, out Position _, targetEntity));
        }

        [Test]
        public void TryGetComponent_WithInvalidTarget_ReturnsFalse()
        {
            var entity = _world.Spawn().Id();
            var invalidTarget = new Entity(new Identity(888)); // Assuming 888 is an invalid ID
            Assert.That(_world.TryGetComponent(entity, out Position _, invalidTarget), Is.False);
        }


        [Test]
        public void AddComponent_SuccessfullyAdded()
        {
            var entity = _world.Spawn().Id();
            _world.AddComponent<Position>(entity);
            Assert.IsTrue(_world.HasComponent<Position>(entity));
        }

        [Test]
        public void AddComponent_NonExistentEntity_Thrown()
        {
            var entity = new Entity(new Identity(999)); // Assuming 999 is an invalid ID
            Assert.Catch<Exception>(() => _world.AddComponent<Position>(entity));
        }

        [Test]
        public void AddComponent_MultipleTimes_Throw()
        {
            var entity = _world.Spawn().Id();
            _world.AddComponent(entity, new Position());
            Assert.Catch<Exception>(() => _world.AddComponent(entity, new Position()));
        }

        [Test]
        public void AddComponent_WithInstance_SuccessfullyAdded()
        {
            var entity = _world.Spawn().Id();
            var position = new Position();
            _world.AddComponent(entity, position);
            var retrievedPosition = _world.GetComponent<Position>(entity);
            Assert.That(retrievedPosition, Is.SameAs(position));
        }

        [Test]
        public void AddComponent_WithInstance_NonExistentEntity_Thrown()
        {
            var entity = new Entity(new Identity(999)); // Assuming 999 is an invalid ID
            var position = new Position();
            Assert.Catch<Exception>(() => _world.AddComponent(entity, position));
        }


        [Test]
        public void GetTarget_SuccessfullyRetrieved()
        {
            var entity = _world.Spawn().Id();
            var targetEntity = _world.Spawn().Id();
            _world.AddComponent<Position>(entity, targetEntity);
            var retrievedTarget = _world.GetTarget<Position>(entity);
            Assert.That(retrievedTarget, Is.EqualTo(targetEntity));
        }

        [Test]
        public void GetTarget_NonExistentEntity_ReturnsNull()
        {
            var entity = new Entity(new Identity(999)); // Assuming 999 is an invalid ID
            Assert.Catch<Exception>(() => _world.GetTarget<Position>(entity));
        }

        [Test]
        public void GetTarget_EntityWithoutComponent_ReturnsNull()
        {
            var entity = _world.Spawn().Id();
            var target = _world.GetTarget<Position>(entity);
            Assert.That(target.IsNone, Is.True);
        }

        [Test]
        public void GetTargets_SuccessfullyRetrieved()
        {
            var entity = _world.Spawn().Id();
            var targetEntity1 = _world.Spawn().Id();
            var targetEntity2 = _world.Spawn().Id();
            _world.AddComponent<Position>(entity, targetEntity1);
            _world.AddComponent<Position>(entity, targetEntity2);
            var retrievedTargets = _world.GetTargets<Position>(entity);
            Assert.That(retrievedTargets, Is.EquivalentTo(new[] { targetEntity1, targetEntity2 }));
        }

        [Test]
        public void GetTargets_NonExistentEntity_ReturnsEmptyList()
        {
            var entity = new Entity(new Identity(999)); // Assuming 999 is an invalid ID
            Assert.Catch<Exception>(() => _world.GetTargets<Position>(entity));
        }

        [Test]
        public void GetTargets_EntityWithoutComponent_ReturnsEmptyList()
        {
            var entity = _world.Spawn().Id();
            var targets = _world.GetTargets<Position>(entity);
            Assert.That(targets, Is.Empty);
        }

        [Test]
        public void GetComponents_SuccessfullyRetrieved()
        {
            var entity = _world.Spawn().Id();
            var position = new Position();
            var velocity = new Velocity();
            _world.AddComponent(entity, position);
            _world.AddComponent(entity, velocity);
            var components = _world.GetComponents(entity).Select(t => t.Item2);
            Assert.That(components, Is.EquivalentTo(new object[] { entity, position, velocity }));
        }

        [Test]
        public void GetComponents_NonExistentEntity_ReturnsEmptyList()
        {
            var entity = new Entity(new Identity(999)); // Assuming 999 is an invalid ID
            Assert.Catch<Exception>(() => _world.GetComponents(entity));
        }

        [Test]
        public void GetComponents_EntityWithoutComponents_ReturnsEmptyList()
        {
            var entity = _world.Spawn().Id();
            var components = _world.GetComponents(entity).Select(t => t.Item2);
            Assert.That(components, Is.EquivalentTo(new [] { entity }));
        }

        [Test]
        public void GetComponents_AfterRemovingComponent_ExcludesRemovedComponent()
        {
            var entity = _world.Spawn().Id();
            var position = new Position();
            var velocity = new Velocity();
            _world.AddComponent(entity, position);
            _world.AddComponent(entity, velocity);
            _world.RemoveComponent<Position>(entity);
            var components = _world.GetComponents(entity).Select(t => t.Item2);
            Assert.That(components, Is.EquivalentTo(new object[] { entity, velocity }));
        }


        [Test]
        public void GetElement_SuccessfullyRetrieved()
        {
            var element = new SomeElement();
            _world.AddElement(element);
            Assert.That(_world.GetElement<SomeElement>(), Is.SameAs(element));
        }

        [Test]
        public void TryGetElement_SuccessfullyRetrieved()
        {
            var element = new SomeElement();
            _world.AddElement(element);
            Assert.That(_world.TryGetElement(out SomeElement retrievedElement), Is.True);
            Assert.That(retrievedElement, Is.SameAs(element));
        }

        [Test]
        public void TryGetElement_NoElement_ReturnsFalse()
        {
            Assert.That(_world.TryGetElement(out SomeElement _), Is.False);
        }

        [Test]
        public void HasElement_ElementExists_ReturnsTrue()
        {
            var element = new SomeElement();
            _world.AddElement(element);
            Assert.That(_world.HasElement<SomeElement>(), Is.True);
        }

        [Test]
        public void HasElement_NoElement_ReturnsFalse()
        {
            Assert.That(_world.HasElement<SomeElement>(), Is.False);
        }

        [Test]
        public void AddElement_SuccessfullyAdded()
        {
            var element = new SomeElement();
            _world.AddElement(element);
            Assert.That(_world.GetElement<SomeElement>(), Is.EqualTo(element));
        }

        [Test]
        public void ReplaceElement_SuccessfullyReplaced()
        {
            var element1 = new SomeElement();
            var element2 = new SomeElement();
            _world.AddElement(element1);
            _world.ReplaceElement(element2);
            Assert.That(_world.GetElement<SomeElement>(), Is.SameAs(element2));
        }

        [Test]
        public void AddOrReplaceElement_AddsWhenNotExists()
        {
            var element = new SomeElement();
            _world.AddOrReplaceElement(element);
            Assert.That(_world.GetElement<SomeElement>(), Is.SameAs(element));
        }

        [Test]
        public void AddOrReplaceElement_ReplacesWhenExists()
        {
            var element1 = new SomeElement();
            var element2 = new SomeElement();
            _world.AddElement(element1);
            _world.AddOrReplaceElement(element2);
            Assert.That(_world.GetElement<SomeElement>(), Is.SameAs(element2));
        }

        [Test]
        public void RemoveElement_SuccessfullyRemoved()
        {
            var element = new SomeElement();
            _world.AddElement(element);
            _world.RemoveElement<SomeElement>();
            Assert.That(_world.HasElement<SomeElement>(), Is.False);
        }

        [Test]
        public void AddObjectComponent_SuccessfullyAdded()
        {
            var entity = _world.Spawn().Id();
            var position = new Position();
            _world.AddObjectComponent(entity, position);
            Assert.That(_world.HasComponent<Position>(entity), Is.True);
        }

        [Test]
        public void AddObjectComponent_NullComponent_ThrowsException()
        {
            var entity = _world.Spawn().Id();
            Assert.Catch<Exception>(() => _world.AddObjectComponent(entity, null!));
        }

        [Test]
        public void AddObjectComponent_NonExistentEntity_Thrown()
        {
            var entity = new Entity(new Identity(999)); // Assuming 999 is an invalid ID
            var position = new Position();
            Assert.Catch<Exception>(() => _world.AddObjectComponent(entity, position));
        }

        [Test]
        public void AddObjectComponent_MultipleTimes_OverwritesComponent()
        {
            var entity = _world.Spawn().Id();
            var position1 = new Position();
            var position2 = new Position();
            _world.AddObjectComponent(entity, position1);
            Assert.Catch<Exception>(() => _world.AddObjectComponent(entity, position2));
        }

        [Test]
        public void Query_BasicQuery_ReturnsAllEntities()
        {
            var entity1 = _world.Spawn().Id();
            var entity2 = _world.Spawn().Id();
            var results = _world.Query<Entity>().Build().GetEnumerator().Count();
            Assert.That(results, Is.EqualTo(2 + 1/* world entity*/));
        }

        [Test]
        public void Query_SingleComponent_ReturnsEntitiesWithComponent()
        {
            var entity1 = _world.Spawn().Add(new Position(1, 1)).Id();
            var entity2 = _world.Spawn().Id();
            var count = _world.Query<Position>().Build().GetEnumerator().Count();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Query_MultipleComponents_ReturnsEntitiesWithComponents()
        {
            var entity1 = _world.Spawn().Add(new Position(1, 1)).Add(new Velocity(2, 2)).Id();
            var entity2 = _world.Spawn().Add(new Position(1, 1)).Id();
            var count = _world.Query<Position, Velocity>().Build().GetEnumerator().Count();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Query_NonExistentComponent_ReturnsEmpty()
        {
            var entity1 = _world.Spawn().Add(new Position(1, 1)).Id();
            var results = _world.Query<SomeNonExistentComponent>().Build().GetEnumerator().AsEnumerable();
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void Query_ChainingHasConditions_ReturnsEntitiesWithMultipleComponents()
        {
            var entity1 = _world.Spawn().Add(new Position(1, 1)).Add(new Velocity(2, 2)).Id();
            var entity2 = _world.Spawn().Add(new Position(1, 1)).Id();
            var results = _world.Query().Has<Position>().Has<Velocity>().Build().GetEnumerator().AsEnumerable();
            Assert.That(results, Is.EquivalentTo(new [] { entity1 }));
        }

        [Test]
        public void Query_ChainingHasAndWithoutConditions_ReturnsEntitiesWithSpecificComponents()
        {
            var entity1 = _world.Spawn().Add(new Position(1, 1)).Add(new Velocity(2, 2)).Id();
            var entity2 = _world.Spawn().Add(new Position(1, 1)).Id();
            var results = _world.Query().Has<Position>().Not<Velocity>().Build().GetEnumerator().AsEnumerable();
            Assert.That(results, Is.EquivalentTo(new [] { entity2 }));
        }

        [Test]
        public void Query_ChainingMultipleWithoutConditions_ReturnsEntitiesWithoutSpecificComponents()
        {
            var entity1 = _world.Spawn().Add(new Position(1, 1)).Id();
            var entity2 = _world.Spawn().Add(new Velocity(2, 2)).Id();
            var results = _world.Query().Not<Position>().Not<Velocity>().Build().GetEnumerator().AsEnumerable();
            Assert.That(results, Is.EquivalentTo(new [] { _world._world }));
        }
    }
}
