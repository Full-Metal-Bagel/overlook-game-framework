using NUnit.Framework;

namespace Overlook.Ecs.Tests
{
    public class QueryPointerTests
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
        public void AddUnmanagedComponent_SuccessfullyAdded()
        {
            var entity = _world.Spawn();
            var component = new UnmanagedComponent();
            _world.AddComponent(entity, component);
            Assert.That(_world.HasComponent<UnmanagedComponent>(entity), Is.True);
        }
    }
}
