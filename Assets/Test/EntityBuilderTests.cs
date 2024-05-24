using NUnit.Framework;

namespace RelEcs.Tests
{
    [TestFixture]
    public class EntityBuilderTests
    {
        private World _world;
        private Entity _entity;

        [SetUp]
        public void Setup()
        {
            _world = new World();
            _entity = _world.Spawn();
        }

        [Test]
        public void Add_Component_AddsComponentToEntity()
        {
            EntityBuilder.Create().Add(new object()).Build(_world);
            Assert.That(_world.HasComponent<object>(_entity), Is.True);
        }

        [Test]
        public void Add_ComponentWithData_AddsComponentToEntity()
        {
            EntityBuilder.Create().Add(new object()).Build(_world);
            Assert.That(_world.HasComponent<object>(_entity), Is.True);
        }

        [Test]
        public void Add_ComponentTwice_KeepFirstOne()
        {
            var first = new object();
            var second = new object();
            var entity = EntityBuilder.Create().Add(first).Build(_world);
            _world.AddComponent(entity, second);
            Assert.That(_world.GetComponent<object>(entity), Is.EqualTo(first));
        }
    }
}

