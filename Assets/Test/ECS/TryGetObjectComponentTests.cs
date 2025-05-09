#nullable enable

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Overlook.Ecs.Tests
{
    [TestFixture]
    public class TryGetObjectComponentTests
    {
        private World _world = default!;

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
        public void TryGetObjectComponent_EmptyList_ReturnsFalse()
        {
            // Prepare an entity
            var entity = _world.Spawn();

            // Add and then remove a component to create an empty list
            var component = _world.AddObjectComponent<TestComponent>(entity);
            _world.RemoveObjectComponent<TestComponent>(entity);

            // Manually add the type to the entity without a component instance
            _world.Archetypes.AddDefaultComponent(entity.Identity, typeof(TestComponent));

            // TryGetObjectComponent should return false for an empty list
            Assert.That(_world.TryGetObjectComponent(entity, out TestComponent? retrievedComponent), Is.False);
            Assert.That(retrievedComponent, Is.Null);
        }

        [Test]
        public void TryGetObjectComponent_WithComponents_ReturnsLastComponent()
        {
            // Prepare an entity
            var entity = _world.Spawn();

            // Add multiple components
            var component1 = new TestComponent { Name = "First" };
            var component2 = new TestComponent { Name = "Second" };
            var component3 = new TestComponent { Name = "Third" };

            _world.AddObjectComponent(entity, component1);
            var component2Added = _world.AddMultipleObjectComponent(entity, component2);
            var component3Added = _world.AddMultipleObjectComponent(entity, component3);

            // Verify they were added
            Assert.That(component2Added, Is.SameAs(component2));
            Assert.That(component3Added, Is.SameAs(component3));

            // TryGetObjectComponent should return the last component (component3)
            Assert.That(_world.TryGetObjectComponent(entity, out TestComponent? retrievedComponent), Is.True);
            Assert.That(retrievedComponent, Is.Not.Null);
            Assert.That(retrievedComponent!.Name, Is.EqualTo("Third"));
        }

        // To verify integration with FindObjectComponents
        [Test]
        public void FindObjectComponents_WithMultipleComponents_ReturnsAllComponents()
        {
            // Prepare an entity
            var entity = _world.Spawn();

            // Add multiple components
            var component1 = new TestComponent { Name = "First" };
            var component2 = new TestComponent { Name = "Second" };
            var component3 = new TestComponent { Name = "Third" };

            _world.AddObjectComponent(entity, component1);
            _world.AddMultipleObjectComponent(entity, component2);
            _world.AddMultipleObjectComponent(entity, component3);

            // Use FindObjectComponents to get all components
            var allComponents = new List<TestComponent>();
            _world.FindObjectComponents(entity, allComponents);

            // Verify we got all three components
            Assert.That(allComponents.Count, Is.EqualTo(3));
            Assert.That(allComponents, Contains.Item(component1));
            Assert.That(allComponents, Contains.Item(component2));
            Assert.That(allComponents, Contains.Item(component3));
        }

        // Test component class
        private class TestComponent
        {
            public string Name { get; set; } = "Default";

            public override string ToString()
            {
                return $"TestComponent: {Name}";
            }
        }
    }
}
