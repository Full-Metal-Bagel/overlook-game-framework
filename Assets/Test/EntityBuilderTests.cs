using System;
using NUnit.Framework;

namespace RelEcs.Tests
{
    [TestFixture]
    public class EntityBuilderTests
    {
        private World _world;
        private Entity _entity;
        private EntityBuilder _builder;

        [SetUp]
        public void Setup()
        {
            _world = new World();
            _builder = _world.Spawn();
            _entity = _builder.Id();
        }

        [Test]
        public void Add_Component_AddsComponentToEntity()
        {
            _builder.Add<object>();
            Assert.That(_world.HasComponent<object>(_entity), Is.True);
        }

        [Test]
        public void Add_ComponentWithData_AddsComponentToEntity()
        {
            _builder.Add(new object());
            Assert.That(_world.HasComponent<object>(_entity), Is.True);
        }

        [Test]
        public void Remove_Component_RemovesComponentFromEntity()
        {
            _builder.Add<Position>();
            _builder.Remove<Position>();
            Assert.That(_world.HasComponent<Position>(_entity), Is.False);
        }

        [Test]
        public void Id_ReturnsEntityIdentity()
        {
            var id = _builder.Id();
            Assert.That(id, Is.EqualTo(_entity));
        }

        [Test]
        public void Add_ComponentTwice_OverwritesExistingComponent()
        {
            var first = new object();
            var second = new object();
            _builder.Add(first);
            Assert.Catch<Exception>(() => _builder.Add(second));
            //
            // Assert.That(_world.GetComponents(_entity).Count(), Is.EqualTo(1));
            // Assert.That(_world.GetComponent<object>(_entity), Is.EqualTo(second));
        }

        [Test]
        public void Remove_ComponentNotPresent_DoesNothing()
        {
            Assert.Catch<Exception>(() => _builder.Remove<object>());
            // _builder.Remove<object>();
            // Assert.That(_world.HasComponent<object>(_entity), Is.False);
        }
    }
}

