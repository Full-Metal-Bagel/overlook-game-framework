#nullable disable

using System.Linq;
using NUnit.Framework;

namespace RelEcs.Tests
{
    [TestFixture]
    public class AbstractComponentTests
    {
        private World _world;

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
            var query = _world.Query().Has<I>().Build();
            Assert.That(query.AsEnumerable().Select(e => query.Get<I>(e)), Is.EquivalentTo(new [] { instance }));
        }

        [Test]
        public void should_get_component_by_generic_interface()
        {
            _world.Spawn().Add<C>();
            Assert.That(_world.Query().Has<I>().Build().Count(), Is.EqualTo(1));
        }

        [Test]
        public void should_get_component_by_base_type()
        {
            var instance = new CC();
            _world.Spawn().Add(instance);
            Assert.That(_world.Query().Has<C>().Build().AsEnumerable<C>(), Is.EquivalentTo(new [] { instance }));
        }

        [Test]
        public void should_get_component_by_generic_base_type()
        {
            _world.Spawn().Add<CC>();
            Assert.That(_world.Query().Has<C>().Build().Count(), Is.EqualTo(1));
        }

        [Test]
        public void should_get_given_component()
        {
            var c = new C();
            var cc = new CC();
            _world.Spawn().Add(c).Add(cc);
            Assert.That(_world.Query().Has<C>().Build().AsEnumerable<C>(), Is.EquivalentTo(new [] { c }));
            Assert.That(_world.Query().Has<CC>().Build().AsEnumerable<CC>(), Is.EquivalentTo(new [] { cc }));
        }

        [Test]
        public void should_get_given_component_2()
        {
            var c = new C();
            var cc = new CC();
            _world.Spawn().Add(cc).Add(c);
            Assert.That(_world.Query().Has<C>().Build().AsEnumerable<C>(), Is.EquivalentTo(new [] { c }));
            Assert.That(_world.Query().Has<CC>().Build().AsEnumerable<CC>(), Is.EquivalentTo(new [] { cc }));
        }

        [Test]
        public void should_get_given_component_with_not_1()
        {
            var c = new C();
            _world.Spawn().Add(c);
            Assert.That(_world.Query().Has<C>().Not<CC>().Build().AsEnumerable<C>(), Is.EquivalentTo(new [] { c }));
            Assert.That(_world.Query().Has<CC>().Not<C>().Build().AsEnumerable<CC>(), Is.Empty);
        }

        [Test]
        public void should_get_given_component_with_not_2()
        {
            var cc = new CC();
            _world.Spawn().Add(cc);
            Assert.That(_world.Query().Has<C>().Not<CC>().Build().AsEnumerable<C>(), Is.Empty);
            Assert.That(_world.Query().Has<CC>().Not<C>().Build().AsEnumerable<CC>(), Is.Empty);
        }

        [Test]
        public void should_get_multi_components()
        {
            var c = new C();
            var cc = new CC();
            _world.Spawn().Add(cc);
            _world.Spawn().Add(c);
            _world.Spawn().Add(c).Add(cc);
            Assert.That(_world.Query().Has<C>().Build().Count(), Is.EqualTo(3));
            Assert.That(_world.Query().Has<CC>().Build().Count(), Is.EqualTo(2));
            Assert.That(_world.Query().Has<C>().Not<CC>().Build().Count(), Is.EqualTo(1));
            Assert.That(_world.Query().Has<CC>().Not<C>().Build().Count(), Is.EqualTo(0));
        }
    }
}
