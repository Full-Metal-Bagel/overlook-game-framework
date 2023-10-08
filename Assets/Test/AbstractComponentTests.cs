#nullable disable

using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace RelEcs.Tests
{
    [TestFixture]
    public class AbstractComponentTests
    {
        private World _world;

        interface I {}
        interface II {}
        class C : I {}
        sealed class Cc : C, II {}

        [SetUp]
        public void Setup()
        {
            _world = new World();
        }

        [Test]
        public void should_get_component_by_interface()
        {
            var instance = new C();
            _world.Spawn().Add(instance);
            Assert.That(_world.Query<I>().Build().AsEnumerable(), Is.EquivalentTo(new [] { instance }));
        }

        [Test]
        public void should_get_component_by_generic_interface()
        {
            _world.Spawn().Add<C>();
            Assert.That(_world.Query<I>().Build().Count(), Is.EqualTo(1));
        }

        [Test]
        public void should_get_component_by_base_type()
        {
            var instance = new Cc();
            _world.Spawn().Add(instance);
            Assert.That(_world.Query<C>().Build().AsEnumerable(), Is.EquivalentTo(new [] { instance }));
        }

        [Test]
        public void should_get_component_by_generic_base_type()
        {
            _world.Spawn().Add<Cc>();
            Assert.That(_world.Query<C>().Build().Count(), Is.EqualTo(1));
        }

        [Test]
        public void should_get_given_component()
        {
            var c = new C();
            var cc = new Cc();
            _world.Spawn().Add(c).Add(cc);
            Assert.That(_world.Query<C>().Build().AsEnumerable(), Is.EquivalentTo(new [] { c }));
            Assert.That(_world.Query<Cc>().Build().AsEnumerable(), Is.EquivalentTo(new [] { cc }));
        }

        [Test]
        public void should_get_given_component_2()
        {
            var c = new C();
            var cc = new Cc();
            _world.Spawn().Add(cc).Add(c);
            Assert.That(_world.Query<C>().Build().AsEnumerable(), Is.EquivalentTo(new [] { c }));
            Assert.That(_world.Query<Cc>().Build().AsEnumerable(), Is.EquivalentTo(new [] { cc }));
        }

        [Test]
        public void should_get_given_component_with_not_1()
        {
            var c = new C();
            _world.Spawn().Add(c);
            Assert.That(_world.Query<C>().Not<Cc>().Build().AsEnumerable(), Is.EquivalentTo(new [] { c }));
            Assert.That(_world.Query<Cc>().Not<C>().Build().AsEnumerable(), Is.Empty);
        }

        [Test]
        public void should_get_given_component_with_not_2()
        {
            var cc = new Cc();
            _world.Spawn().Add(cc);
            Assert.That(_world.Query<C>().Not<Cc>().Build().AsEnumerable(), Is.Empty);
            Assert.That(_world.Query<Cc>().Not<C>().Build().AsEnumerable(), Is.Empty);
        }

        [Test]
        public void should_get_multi_components()
        {
            var c = new C();
            var cc = new Cc();
            _world.Spawn().Add(cc);
            _world.Spawn().Add(c);
            _world.Spawn().Add(c).Add(cc);
            Assert.That(_world.Query<C>().Build().Count(), Is.EqualTo(3));
            Assert.That(_world.Query<Cc>().Build().Count(), Is.EqualTo(2));
            Assert.That(_world.Query<C>().Not<Cc>().Build().Count(), Is.EqualTo(1));
            Assert.That(_world.Query<Cc>().Not<C>().Build().Count(), Is.EqualTo(0));
        }
    }
}
