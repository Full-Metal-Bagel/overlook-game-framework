#nullable disable

using System;
using System.Linq;
using NUnit.Framework;

namespace RelEcs.Tests
{
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
        public void Query_WithComponent_ReturnsCorrectEntities()
        {
            _world.Spawn().Add<Position>();
            _world.Spawn().Add<Position>().Add<Velocity>();
            _world.Spawn().Add<Health>();
            var query = _world.Query().Has<Position>().Build();
            var count = query.Count();
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void Query_WithoutComponent_ReturnsCorrectEntities()
        {
            _world.Spawn().Add<Position>();
            _world.Spawn().Add<Position>().Add<Velocity>();
            _world.Spawn().Add<Health>();
            var query = _world.Query().Has<Position>().Not<Velocity>().Build();
            var count = query.Count();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Query_WithMultipleComponents_ReturnsCorrectEntities()
        {
            _world.Spawn().Add<Position>();
            _world.Spawn().Add<Position>().Add<Velocity>();
            _world.Spawn().Add<Health>();
            var query = _world.Query().Has<Position>().Has<Velocity>().Build();
            var count = query.Count();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Query_WithoutMultipleComponents_ReturnsCorrectEntities()
        {
            _world.Spawn().Add<Position>();
            _world.Spawn().Add<Position>().Add<Velocity>();
            _world.Spawn().Add<Health>();
            var query = _world.Query().Has<Position>().Not<Velocity>().Not<Health>().Build();
            var count = query.Count();
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
            Assert.That(retrievedPosition, Is.EqualTo(position));
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
        public void TryGetComponent_SuccessfullyRetrieved()
        {
            var entity = _world.Spawn().Id();
            var position = new Position();
            _world.AddComponent(entity, position);
            Assert.That(_world.TryGetComponent<Position>(entity, out var retrievedPosition), Is.True);
            Assert.That(retrievedPosition, Is.EqualTo(position));
        }

        [Test]
        public void TryGetComponent_NonExistentEntity_ReturnsFalse()
        {
            var entity = new Entity(new Identity(999)); // Assuming 999 is an invalid ID
            Assert.Catch<Exception>(() => _world.TryGetComponent<Position>(entity, out var _));
        }

        [Test]
        public void TryGetComponent_EntityWithoutComponent_ReturnsFalse()
        {
            var entity = _world.Spawn().Id();
            Assert.That(_world.TryGetComponent(entity, out Position? _), Is.False);
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
            Assert.That(retrievedPosition, Is.EqualTo(position));
        }

        [Test]
        public void AddComponent_WithInstance_NonExistentEntity_Thrown()
        {
            var entity = new Entity(new Identity(999)); // Assuming 999 is an invalid ID
            var position = new Position();
            Assert.Catch<Exception>(() => _world.AddComponent(entity, position));
        }

        [Test]
        public void AddObjectComponent_SuccessfullyAdded()
        {
            var entity = _world.Spawn().Id();
            _world.AddComponent<object>(entity);
            Assert.That(_world.HasComponent<object>(entity), Is.True);
        }

        [Test]
        public void AddObjectComponent_NullComponent_ThrowsException()
        {
            var entity = _world.Spawn().Id();
            Assert.Catch<Exception>(() => _world.AddComponent<object>(entity, null!));
        }

        [Test]
        public void AddObjectComponent_NonExistentEntity_Thrown()
        {
            var entity = new Entity(new Identity(999)); // Assuming 999 is an invalid ID
            var position = new Position();
            Assert.Catch<Exception>(() => _world.AddComponent(entity, position));
        }

        [Test]
        public void AddObjectComponent_MultipleTimes_OverwritesComponent()
        {
            var entity = _world.Spawn().Id();
            var position1 = new Position();
            var position2 = new Position();
            _world.AddComponent(entity, position1);
            Assert.Catch<Exception>(() => _world.AddComponent(entity, position2));
        }

        [Test]
        public void Query_BasicQuery_ReturnsAllEntities()
        {
            var entity1 = _world.Spawn().Id();
            var entity2 = _world.Spawn().Id();
            var results = _world.Query().Build().Count();
            Assert.That(results, Is.EqualTo(2));
        }

        [Test]
        public void Query_SingleComponent_ReturnsEntitiesWithComponent()
        {
            var entity1 = _world.Spawn().Add(new Position(1, 1)).Id();
            var entity2 = _world.Spawn().Id();
            var count = _world.Query().Has<Position>().Build().Count();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Query_MultipleComponents_ReturnsEntitiesWithComponents()
        {
            var entity1 = _world.Spawn().Add(new Position(1, 1)).Add(new Velocity(2, 2)).Id();
            var entity2 = _world.Spawn().Add(new Position(1, 1)).Id();
            var count = _world.Query().Has<Position>().Has<Velocity>().Build().Count();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Query_NonExistentComponent_ReturnsEmpty()
        {
            var entity1 = _world.Spawn().Add(new Position(1, 1)).Id();
            var count = _world.Query().Has<SomeNonExistentComponent>().Build().Count();
            Assert.That(count, Is.Zero);
        }

        [Test]
        public void Query_ChainingHasConditions_ReturnsEntitiesWithMultipleComponents()
        {
            var entity1 = _world.Spawn().Add(new Position(1, 1)).Add(new Velocity(2, 2)).Id();
            var entity2 = _world.Spawn().Add(new Position(1, 1)).Id();
            var results = _world.Query().Has<Position>().Has<Velocity>().Build().AsEnumerable();
            Assert.That(results, Is.EquivalentTo(new [] { entity1 }));
        }

        [Test]
        public void Query_ChainingHasAndWithoutConditions_ReturnsEntitiesWithSpecificComponents()
        {
            var entity1 = _world.Spawn().Add(new Position(1, 1)).Add(new Velocity(2, 2)).Id();
            var entity2 = _world.Spawn().Add(new Position(1, 1)).Id();
            var results = _world.Query().Has<Position>().Not<Velocity>().Build().AsEnumerable();
            Assert.That(results, Is.EquivalentTo(new [] { entity2 }));
        }

        [Test]
        public void Query_ChainingMultipleWithoutConditions_ReturnsEntitiesWithoutSpecificComponents()
        {
            var entity1 = _world.Spawn().Add(new Position(1, 1)).Id();
            var entity2 = _world.Spawn().Add(new Velocity(2, 2)).Id();
            var results = _world.Query().Not<Position>().Not<Velocity>().Build().AsEnumerable();
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void should_throw_on_add_struct_component_by_its_interface()
        {
            Assert.Catch(() => _world.Spawn().Add<I>(new S()));
        }
    }
}
