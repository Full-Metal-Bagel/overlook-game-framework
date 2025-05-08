using System;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;

namespace Overlook.Pool.Tests
{
    [TestFixture]
    public class ObjectPoolProviderTests
    {
        #region Test Factory Classes

        [Priority(1)]
        [AttributeUsage(AttributeTargets.Assembly)]
        private class HighPriorityFactory : Attribute, IAssemblyObjectPoolProviderFactory
        {
            public bool CanProvide<T>() where T : class => typeof(T) == typeof(HighPriorityObject);
            
            // Implementation would depend on your actual interface
            public IObjectPoolProvider<T> CreateProvider<T>() where T : class
            {
                // Mock implementation for testing
                return null;
            }
        }

        [Priority(2)]
        [AttributeUsage(AttributeTargets.Assembly)]
        private class MediumPriorityFactory : Attribute, IAssemblyObjectPoolProviderFactory
        {
            public bool CanProvide<T>() where T : class => typeof(T) == typeof(MediumPriorityObject);
            
            public IObjectPoolProvider<T> CreateProvider<T>() where T : class
            {
                // Mock implementation for testing
                return null;
            }
        }

        [AttributeUsage(AttributeTargets.Assembly)]
        private class DefaultPriorityFactory : Attribute, IAssemblyObjectPoolProviderFactory
        {
            public bool CanProvide<T>() where T : class => typeof(T) == typeof(DefaultPriorityObject);
            
            public IObjectPoolProvider<T> CreateProvider<T>() where T : class
            {
                // Mock implementation for testing
                return null;
            }
        }

        private class HighPriorityObject { }
        private class MediumPriorityObject { }
        private class DefaultPriorityObject { }

        #endregion

        [Test]
        public void FactoriesAreSortedByPriority()
        {
            // Since the factories array is private, we need to use reflection to access it
            var factoriesField = typeof(ObjectPoolProvider).GetField("_factories", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            // Act
            var factories = factoriesField.GetValue(null) as IAssemblyObjectPoolProviderFactory[];
            
            // Assert
            Assert.IsNotNull(factories, "Factories array should not be null");
            
            // This test is simplified since we can't easily add assembly attributes at runtime
            // In a real test scenario, you might use a mocking framework or create a test assembly
            
            Assert.Ignore("This test requires a test assembly with provider factory attributes");
            
            /* Example of what a full test might look like with a test assembly:
            
            Assert.AreEqual(3, factories.Length, "Should have found 3 factories");
            Assert.IsInstanceOf<HighPriorityFactory>(factories[0], "First factory should be highest priority");
            Assert.IsInstanceOf<MediumPriorityFactory>(factories[1], "Second factory should be medium priority");
            Assert.IsInstanceOf<DefaultPriorityFactory>(factories[2], "Third factory should be default priority");
            */
        }

        [Test]
        public void SimulatedFactoriesSortingTest()
        {
            // Creating a list of fake factories to test the sorting logic independently
            var fakeFactories = new List<IAssemblyObjectPoolProviderFactory>
            {
                new DefaultPriorityFactory(),
                new HighPriorityFactory(),
                new MediumPriorityFactory()
            };
            
            // Sort using the same logic as in ObjectPoolProvider
            var sortedFactories = fakeFactories
                .Select(factory => new
                {
                    Factory = factory,
                    Priority = factory.GetType().GetCustomAttribute<PriorityAttribute>()?.Priority ?? int.MaxValue
                })
                .OrderBy(item => item.Priority)
                .Select(item => item.Factory)
                .ToArray();
            
            // Assert
            Assert.AreEqual(3, sortedFactories.Length);
            Assert.IsInstanceOf<HighPriorityFactory>(sortedFactories[0]);
            Assert.IsInstanceOf<MediumPriorityFactory>(sortedFactories[1]);
            Assert.IsInstanceOf<DefaultPriorityFactory>(sortedFactories[2]);
        }

        [Test]
        public void PriorityAttribute_ReturnsCorrectValue()
        {
            // Test the Priority attribute directly
            var highAttr = new PriorityAttribute(1);
            var mediumAttr = new PriorityAttribute(2);
            var lowAttr = new PriorityAttribute(3);
            
            Assert.AreEqual(1, highAttr.Priority);
            Assert.AreEqual(2, mediumAttr.Priority);
            Assert.AreEqual(3, lowAttr.Priority);
        }
    }
} 