using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Overlook.Ecs.Tests
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

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
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
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, new Regex(".*"));
            _world.AddComponent(entity, second);
            Assert.That(_world.GetComponent<object>(entity), Is.EqualTo(first));
        }
    }
}

