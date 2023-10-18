using NUnit.Framework;

namespace RelEcs.Tests
{
    public class QueryPointerTests
    {
        private World _world;

        [SetUp]
        public void Setup()
        {
            _world = new World();
        }

        [Test]
        public void AddUnmanagedComponent_SuccessfullyAdded()
        {
            var entity = _world.Spawn().Id();
            var component = new UnmanagedComponent();
            _world.AddComponent(entity, component);
            Assert.That(_world.HasComponent<UnmanagedComponent>(entity), Is.True);
        }
    }
}
