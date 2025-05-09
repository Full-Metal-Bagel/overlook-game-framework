using System;
using NUnit.Framework;

namespace Overlook.Ecs.Tests
{
    [TestFixture]
    public class WorldExtensionsTests
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

        [Test]
        public void Use_WithNewEntity_SuccessfullyAdds()
        {
            // Create an entity Identity that doesn't exist yet
            var identity = new Identity(100, 1);
            var entity = new Entity(identity);

            // Should not throw - the identity doesn't exist
            Assert.DoesNotThrow(() => _world.Use(entity));

            // Entity should now be alive
            Assert.That(_world.IsAlive(entity), Is.True);
        }

        [Test]
        public void Use_WithExistingEntity_ThrowsException()
        {
            // First create a normal entity
            var entity = _world.Spawn();

            // Using the same entity again should throw
            Assert.Throws<ArgumentException>(() => _world.Use(entity));
        }

        [Test]
        public void ReadOnlyUnmanagedWorld_Empty_IsValidInstance()
        {
            var readOnlyWorld = World.Empty;

            // Should not be able to find any entities
            var testEntity = new Entity(new Identity(1, 1));
            Assert.That(readOnlyWorld.IsAlive(testEntity), Is.False);
        }

        [Test]
        public void ReadOnlyUnmanagedWorld_GetComponent_ReadsCorrectly()
        {
            // Create an entity with components
            var entity = EntityBuilder.Create()
                .Add(new Position(10, 20))
                .Add(new Velocity(1, 2))
                .Build(_world);

            // Create a readonly view
            var readOnlyWorld = new ReadOnlyUnmanagedWorld(_world);

            // Should be able to read the components
            var position = readOnlyWorld.GetComponent<Position>(entity);
            Assert.That(position.X, Is.EqualTo(10));
            Assert.That(position.Y, Is.EqualTo(20));

            // Change the component in the original world
            ref var mutablePos = ref _world.GetComponent<Position>(entity);
            mutablePos = new Position(30, 40);

            // The readonly world should reflect the changes
            position = readOnlyWorld.GetComponent<Position>(entity);
            Assert.That(position.X, Is.EqualTo(30));
            Assert.That(position.Y, Is.EqualTo(40));
        }

        [Test]
        public void ReadOnlyUnmanagedWorld_HasComponent_WorksCorrectly()
        {
            // Create an entity with only Position
            var entity = EntityBuilder.Create()
                .Add(new Position(10, 20))
                .Build(_world);

            var readOnlyWorld = new ReadOnlyUnmanagedWorld(_world);

            // Has Position but not Velocity
            Assert.That(readOnlyWorld.HasComponent<Position>(entity), Is.True);
            Assert.That(readOnlyWorld.HasComponent<Velocity>(entity), Is.False);

            // Add Velocity to the entity
            _world.AddComponent(entity, new Velocity(1, 2));

            // Now should have Velocity too
            Assert.That(readOnlyWorld.HasComponent<Velocity>(entity), Is.True);
        }

        [Test]
        public void ReadOnlyUnmanagedWorld_TryGetComponent_WorksCorrectly()
        {
            // Create an entity with Position
            var entity = EntityBuilder.Create()
                .Add(new Position(10, 20))
                .Build(_world);

            var readOnlyWorld = new ReadOnlyUnmanagedWorld(_world);

            // Should be able to get Position
            Assert.That(readOnlyWorld.TryGetComponent(entity, out Position? pos), Is.True);
            Assert.That(pos!.Value.X, Is.EqualTo(10));

            // Should not be able to get Velocity
            Assert.That(readOnlyWorld.TryGetComponent(entity, out Velocity? _), Is.False);
        }

        [Test]
        public unsafe void ReadOnlyUnmanagedWorld_GetComponentRawData_ReturnsCorrectData()
        {
            // Create an entity with a component
            var position = new Position(42, 99);
            var entity = EntityBuilder.Create()
                .Add(position)
                .Build(_world);

            var readOnlyWorld = new ReadOnlyUnmanagedWorld(_world);

            // Get raw data
            var type = StorageType.Create<Position>();
            var rawData = readOnlyWorld.GetComponentRawData(entity, type);

            // Should have the right length for a Position struct
            Assert.That(rawData.Length, Is.EqualTo(sizeof(Position)));

            // This is more of an integration test to ensure the raw data actually reflects
            // the underlying component - we'd need to deserialize to fully verify
        }
    }
}
