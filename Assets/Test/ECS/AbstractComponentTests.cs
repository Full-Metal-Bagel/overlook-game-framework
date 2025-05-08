#nullable disable

using System.Linq;
using NUnit.Framework;

namespace Overlook.Ecs.Tests
{
    [TestFixture]
    public class AbstractComponentTests
    {
        [Test]
        public void should_get_component_by_interface()
        {
            using var world = new World();
            var instance = new C();
            EntityBuilder.Create().Add(instance).Build(world);
            var query = QueryBuilder.Create().Has<I>().Build(world);
            Assert.That(query.AsEnumerable().Select(e => query.Get<I>(e)), Is.EquivalentTo(new [] { instance }));
        }

        [Test]
        public void should_get_component_by_generic_interface()
        {
            using var world = new World();
            EntityBuilder.Create().Add(new C()).Build(world);
            Assert.That(QueryBuilder.Create().Has<I>().Build(world).Count(), Is.EqualTo(1));
        }

        [Test]
        public void should_get_component_by_base_type()
        {
            using var world = new World();
            var instance = new CC();
            EntityBuilder.Create().Add(instance).Build(world);
            Assert.That(QueryBuilder.Create().Has<C>().Build(world).AsEnumerable<C>(), Is.EquivalentTo(new [] { instance }));
        }

        [Test]
        public void should_get_component_by_generic_base_type()
        {
            using var world = new World();
            EntityBuilder.Create().Add(new CC()).Build(world);
            Assert.That(QueryBuilder.Create().Has<C>().Build(world).Count(), Is.EqualTo(1));
        }

        [Test]
        public void should_get_given_component()
        {
            using var world = new World();
            var c = new C();
            var cc = new CC();
            EntityBuilder.Create().Add(c).Add(cc).Build(world);
            Assert.That(QueryBuilder.Create().Has<C>().Build(world).AsEnumerable<C>(), Is.EquivalentTo(new [] { c }));
            Assert.That(QueryBuilder.Create().Has<CC>().Build(world).AsEnumerable<CC>(), Is.EquivalentTo(new [] { cc }));
        }

        [Test]
        public void should_get_given_component_2()
        {
            using var world = new World();
            var c = new C();
            var cc = new CC();
            EntityBuilder.Create().Add(cc).Add(c).Build(world);
            Assert.That(QueryBuilder.Create().Has<C>().Build(world).AsEnumerable<C>(), Is.EquivalentTo(new [] { c }));
            Assert.That(QueryBuilder.Create().Has<CC>().Build(world).AsEnumerable<CC>(), Is.EquivalentTo(new [] { cc }));
        }

        [Test]
        public void should_get_given_component_with_not_1()
        {
            using var world = new World();
            var c = new C();
            EntityBuilder.Create().Add(c).Build(world);
            Assert.That(QueryBuilder.Create().Has<C>().Not<CC>().Build(world).AsEnumerable<C>(), Is.EquivalentTo(new [] { c }));
            Assert.That(QueryBuilder.Create().Has<CC>().Not<C>().Build(world).AsEnumerable<CC>(), Is.Empty);
        }

        [Test]
        public void should_get_given_component_with_not_2()
        {
            using var world = new World();
            var cc = new CC();
            EntityBuilder.Create().Add(cc).Build(world);
            Assert.That(QueryBuilder.Create().Has<C>().Not<CC>().Build(world).AsEnumerable<C>(), Is.Empty);
            Assert.That(QueryBuilder.Create().Has<CC>().Not<C>().Build(world).AsEnumerable<CC>(), Is.Empty);
        }

        [Test]
        public void should_get_multi_components()
        {
            using var world = new World();
            var c = new C();
            var cc = new CC();
            EntityBuilder.Create().Add(cc).Build(world);
            EntityBuilder.Create().Add(c).Build(world);
            EntityBuilder.Create().Add(c).Add(cc).Build(world);
            Assert.That(QueryBuilder.Create().Has<C>().Build(world).Count(), Is.EqualTo(3));
            Assert.That(QueryBuilder.Create().Has<CC>().Build(world).Count(), Is.EqualTo(2));
            Assert.That(QueryBuilder.Create().Has<C>().Not<CC>().Build(world).Count(), Is.EqualTo(1));
            Assert.That(QueryBuilder.Create().Has<CC>().Not<C>().Build(world).Count(), Is.EqualTo(0));
        }
    }
}
