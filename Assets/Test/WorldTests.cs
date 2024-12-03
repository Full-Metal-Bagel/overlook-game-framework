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
            EntityBuilder.Create().Add(new Position()).Build(_world);
            EntityBuilder.Create().Add(new Position()).Add(new Velocity()).Build(_world);
            EntityBuilder.Create().Add(new Health()).Build(_world);
            var query = QueryBuilder.Create().Has<Position>().Build(_world);
            var count = query.Count();
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void Query_WithoutComponent_ReturnsCorrectEntities()
        {
            EntityBuilder.Create().Add(new Position()).Build(_world);
            EntityBuilder.Create().Add(new Position()).Add(new Velocity()).Build(_world);
            EntityBuilder.Create().Add(new Health()).Build(_world);
            var query = QueryBuilder.Create().Has<Position>().Not<Velocity>().Build(_world);
            var count = query.Count();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Query_Any()
        {
            var a = EntityBuilder.Create().Add(new Position()).Build(_world);
            var b = EntityBuilder.Create().Add(new Position()).Add(new Velocity()).Build(_world);
            var c = EntityBuilder.Create().Add(new Health()).Build(_world);
            var query = QueryBuilder.Create().Any<Velocity>().Any<Health>().Build(_world);
            Assert.That(query.AsEnumerable(), Is.EquivalentTo(new [] { b, c }));
        }

        [Test]
        public void Query_WithMultipleComponents_ReturnsCorrectEntities()
        {
            EntityBuilder.Create().Add(new Position()).Build(_world);
            EntityBuilder.Create().Add(new Position()).Add(new Velocity()).Build(_world);
            EntityBuilder.Create().Add(new Health()).Build(_world);
            var query = QueryBuilder.Create().Has<Position>().Has<Velocity>().Build(_world);
            var count = query.Count();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Query_WithoutMultipleComponents_ReturnsCorrectEntities()
        {
            EntityBuilder.Create().Add(new Position()).Build(_world);
            EntityBuilder.Create().Add(new Position()).Add(new Velocity()).Build(_world);
            EntityBuilder.Create().Add(new Health()).Build(_world);
            var query = QueryBuilder.Create().Has<Position>().Not<Velocity>().Not<Health>().Build(_world);
            var count = query.Count();
            Assert.That(count, Is.EqualTo(1));
        }


        [Test]
        public void Despawn_Entity_SuccessfullyDespawned()
        {
            var entity = EntityBuilder.Create().Add(new Position()).Add(new Velocity()).Build(_world);
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
            var entity = EntityBuilder.Create().Add(new Position()).Add(new Velocity()).Build(_world);
            _world.Despawn(entity);
            Assert.DoesNotThrow(() => _world.Despawn(entity));
        }

        [Test]
        public void Despawn_AfterAddingComponents_ComponentsRemoved()
        {
            // Assuming we have a component called Position
            var entity = EntityBuilder.Create().Add(new Position()).Build(_world);
            _world.Despawn(entity);
            Assert.Catch<ArgumentException>(() => _world.HasComponent<Position>(entity));
        }

        [Test]
        public void DespawnAllWith_Position()
        {
            // Assuming we have a component called Position
            var entity1 = EntityBuilder.Create().Add(new Position()).Build(_world);
            var entity2 = EntityBuilder.Create().Add(new Position()).Add(new Velocity()).Build(_world);
            _world.DespawnAllWith<Position>();
            Assert.That(_world.IsAlive(entity1), Is.False);
            Assert.That(_world.IsAlive(entity2), Is.False);
        }

        [Test]
        public void GetComponent_EntityWithComponent_SuccessfullyRetrieved()
        {
            var entity = EntityBuilder.Create().Build(_world);
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
            var entity = EntityBuilder.Create().Build(_world);
            Assert.Catch<Exception>(() => _world.GetComponent<Position>(entity));
        }

        [Test]
        public void GetComponent_AfterRemovingComponent_Thrown()
        {
            var entity = EntityBuilder.Create().Build(_world);
            var position = new Position();
            _world.AddComponent(entity, position);
            _world.RemoveComponent<Position>(entity);
            Assert.Catch<Exception>(() => _world.GetComponent<Position>(entity));
        }


        [Test]
        public void TryGetComponent_SuccessfullyRetrieved()
        {
            var entity = EntityBuilder.Create().Build(_world);
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
            var entity = EntityBuilder.Create().Build(_world);
            Assert.That(_world.TryGetComponent(entity, out Position? _), Is.False);
        }

        [Test]
        public void AddComponent_SuccessfullyAdded()
        {
            var entity = EntityBuilder.Create().Build(_world);
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
        public void AddComponent_MultipleTimes_Overwirte()
        {
            var entity = EntityBuilder.Create().Build(_world);
            _world.AddComponent(entity, new Position(1, 2));
            Assert.That(_world.GetComponent<Position>(entity), Is.EqualTo(new Position(1, 2)));
            _world.AddComponent(entity, new Position(3, 4));
            Assert.That(_world.GetComponent<Position>(entity), Is.EqualTo(new Position(3, 4)));
        }

        [Test]
        public void AddComponent_WithInstance_SuccessfullyAdded()
        {
            var entity = EntityBuilder.Create().Build(_world);
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
            var entity = EntityBuilder.Create().Build(_world);
            _world.AddComponent<object>(entity);
            Assert.That(_world.HasComponent<object>(entity), Is.True);
        }

        [Test]
        public void AddObjectComponent_NullComponent_ThrowsException()
        {
            var entity = EntityBuilder.Create().Build(_world);
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
        public void AddUntypedValueComponent_MultipleTimes_OverwritesComponent()
        {
            var entity = EntityBuilder.Create().Build(_world);
            object position1 = new Position(1, 2);
            object position2 = new Position(3, 4);
            _world.AddComponent(entity, position1);
            Assert.That(_world.GetComponent<Position>(entity), Is.EqualTo(new Position(1, 2)));
            _world.AddComponent(entity, position2);
            Assert.That(_world.GetComponent<Position>(entity), Is.EqualTo(new Position(3, 4)));
        }

        [Test]
        public void Query_BasicQuery_ReturnsAllEntities()
        {
            var entity1 = EntityBuilder.Create().Build(_world);
            var entity2 = EntityBuilder.Create().Build(_world);
            var results = QueryBuilder.Create().Build(_world).Count();
            Assert.That(results, Is.EqualTo(2));
        }

        [Test]
        public void Query_SingleComponent_ReturnsEntitiesWithComponent()
        {
            var entity1 = EntityBuilder.Create().Add(new Position(1, 1)).Build(_world);
            var entity2 = EntityBuilder.Create().Build(_world);
            var count = QueryBuilder.Create().Has<Position>().Build(_world).Count();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Query_MultipleComponents_ReturnsEntitiesWithComponents()
        {
            var entity1 = EntityBuilder.Create().Add(new Position(1, 1)).Add(new Velocity(2, 2)).Build(_world);
            var entity2 = EntityBuilder.Create().Add(new Position(1, 1)).Build(_world);
            var count = QueryBuilder.Create().Has<Position>().Has<Velocity>().Build(_world).Count();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Query_NonExistentComponent_ReturnsEmpty()
        {
            var entity1 = EntityBuilder.Create().Add(new Position(1, 1)).Build(_world);
            var count = QueryBuilder.Create().Has<SomeNonExistentComponent>().Build(_world).Count();
            Assert.That(count, Is.Zero);
        }

        [Test]
        public void Query_ChainingHasConditions_ReturnsEntitiesWithMultipleComponents()
        {
            var entity1 = EntityBuilder.Create().Add(new Position(1, 1)).Add(new Velocity(2, 2)).Build(_world);
            var entity2 = EntityBuilder.Create().Add(new Position(1, 1)).Build(_world);
            var results = QueryBuilder.Create().Has<Position>().Has<Velocity>().Build(_world).AsEnumerable();
            Assert.That(results, Is.EquivalentTo(new [] { entity1 }));
        }

        [Test]
        public void Query_ChainingHasAndWithoutConditions_ReturnsEntitiesWithSpecificComponents()
        {
            var entity1 = EntityBuilder.Create().Add(new Position(1, 1)).Add(new Velocity(2, 2)).Build(_world);
            var entity2 = EntityBuilder.Create().Add(new Position(1, 1)).Build(_world);
            var results = QueryBuilder.Create().Has<Position>().Not<Velocity>().Build(_world).AsEnumerable();
            Assert.That(results, Is.EquivalentTo(new [] { entity2 }));
        }

        [Test]
        public void Query_ChainingMultipleWithoutConditions_ReturnsEntitiesWithoutSpecificComponents()
        {
            var entity1 = EntityBuilder.Create().Add(new Position(1, 1)).Build(_world);
            var entity2 = EntityBuilder.Create().Add(new Velocity(2, 2)).Build(_world);
            var results = QueryBuilder.Create().Not<Position>().Not<Velocity>().Build(_world).AsEnumerable();
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void Query_Where()
        {
            var entity1 = EntityBuilder.Create().Add(new Position(1, 1)).Add(new Velocity(2, 2)).Build(_world);
            var entity2 = EntityBuilder.Create().Add(new Position(1, 2)).Build(_world);
            Assert.That(
                QueryBuilder.Create().Has<Position>().Build(_world).Where<Position>(pos => pos.X == 1).AsEnumerable(),
                Is.EquivalentTo(new [] { entity1, entity2 })
            );
            Assert.That(
                QueryBuilder.Create().Has<Position>().Build(_world).Where<Position>(pos => pos.Y == 1).AsEnumerable(),
                Is.EquivalentTo(new [] { entity1 })
            );
            Assert.That(
                QueryBuilder.Create().Has<Velocity>().Build(_world).Where<Velocity>(velocity => velocity.Y == 2).AsEnumerable(),
                Is.EquivalentTo(new [] { entity1 })
            );
            Assert.That(
                QueryBuilder.Create().Build(_world).Where<Position>(pos => pos.X == 1).Where<Position>(pos => pos.Y == 2).AsEnumerable(),
                Is.EquivalentTo(new [] { entity2 })
            );
            Assert.That(
                QueryBuilder.Create().Build(_world).Where<Position>(pos => pos.X == 1).Where<Velocity>(vel => vel.Y == 1).AsEnumerable(),
                Is.Empty
            );
            Assert.That(
                QueryBuilder.Create().Build(_world).Where<Health>(health => health.Value == 0).AsEnumerable(),
                Is.Empty
            );
        }

        [Test]
        public void ArchetypesChangedDuringQuery()
        {
            // Setup initial entities
            var entity1 = EntityBuilder.Create().Add(new Position()).Build(_world);
            var entity2 = EntityBuilder.Create().Add(new Position()).Build(_world);
            var entity3 = EntityBuilder.Create().Add(new Position()).Build(_world);

            var processedEntities = 0;
            var query = QueryBuilder.Create().Has<Position>().Build(_world);
            var queryV = QueryBuilder.Create().Has<Position>().Has<Velocity>().Build(_world);

            // Iterate through query while making archetype changes
            foreach (var entity in query)
            {
                processedEntities++;

                if (entity == entity1)
                {
                    // Add component to existing entity
                    _world.AddComponent(entity2, new Velocity());
                    Assert.That(queryV.Count(), Is.EqualTo(1));
                }
                else if (entity == entity2)
                {
                    // Remove component from existing entity
                    _world.RemoveComponent<Position>(entity3);
                    Assert.That(query.Count(), Is.EqualTo(2));

                    // Spawn new entity during iteration
                    EntityBuilder.Create().Add(new Position()).Build(_world);
                    Assert.That(query.Count(), Is.EqualTo(3));

                    // Despawn an entity during iteration
                    _world.Despawn(entity1);
                    Assert.That(query.Count(), Is.EqualTo(2));
                }
            }

            // Verify the query processed all relevant entities
            Assert.That(processedEntities, Is.EqualTo(3));

            // Verify final world state
            Assert.That(_world.IsAlive(entity1), Is.False);
            Assert.That(_world.HasComponent<Position>(entity2), Is.True);
            Assert.That(_world.HasComponent<Velocity>(entity2), Is.True);
            Assert.That(_world.HasComponent<Position>(entity3), Is.False);

            // Verify final query results
            Assert.That(query.Count(), Is.EqualTo(2)); // entity2 and the newly spawned entity
        }
    }
}
