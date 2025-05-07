using System;
using NUnit.Framework;

namespace Overlook.Pool.Tests
{
    [TestFixture]
    public class ObjectPoolIntegrationTests
    {
        private class TestItem
        {
            public int Value { get; set; }
        }

        [PoolPolicyAttribute<CustomPoolPolicy>]
        private class PolicyAttributeItem
        {
            public string Name { get; set; }
        }

        private class CustomPoolPolicy : IObjectPoolPolicy
        {
            public int InitCount => 3;
            public int MaxCount => 10;
            
            public int Expand(int size)
            {
                return Math.Min(size + 2, MaxCount);
            }
            
            public object Create()
            {
                return new PolicyAttributeItem { Name = "Created by policy" };
            }
        }

        [Test]
        public void ObjectPool_Get_ReturnsNewInstance()
        {
            // Arrange
            var pool = new ObjectPool<TestItem>();
            
            // Act
            var item = pool.Get();
            
            // Assert
            Assert.IsNotNull(item);
            Assert.IsInstanceOf<TestItem>(item);
        }

        [Test]
        public void ObjectPool_Release_RecyclesObject()
        {
            // Arrange
            var pool = new ObjectPool<TestItem>();
            var item = pool.Get();
            item.Value = 42;
            
            // Act
            pool.Release(item);
            var recycledItem = pool.Get();
            
            // Assert
            Assert.AreSame(item, recycledItem, "Pool should return the same instance");
            Assert.AreEqual(0, recycledItem.Value, "Value should be reset");
        }

        [Test]
        public void ObjectPool_WithAttribute_UsesCustomPolicy()
        {
            // Arrange & Act
            var pool = new ObjectPool<PolicyAttributeItem>();
            
            // Assert - verify the pool uses our custom policy
            var privatePolicy = typeof(ObjectPool<PolicyAttributeItem>)
                .GetField("_policy", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(pool);
                
            Assert.IsInstanceOf<CustomPoolPolicy>(privatePolicy);
            
            // Get an instance to test full behavior
            var item = pool.Get();
            Assert.IsNotNull(item);
            Assert.AreEqual("Created by policy", item.Name);
        }

        [Test]
        public void StaticObjectPool_Get_ReturnsInstance()
        {
            // Arrange & Act
            var item = StaticObjectPool<TestItem>.Get();
            
            // Assert
            Assert.IsNotNull(item);
            Assert.IsInstanceOf<TestItem>(item);
        }

        [Test]
        public void StaticObjectPool_Release_RecyclesObject()
        {
            // Arrange
            var item = StaticObjectPool<TestItem>.Get();
            item.Value = 100;
            
            // Act
            StaticObjectPool<TestItem>.Release(item);
            var recycledItem = StaticObjectPool<TestItem>.Get();
            
            // Assert
            Assert.AreSame(item, recycledItem, "Static pool should recycle the same instance");
            Assert.AreEqual(0, recycledItem.Value, "Value should be reset");
        }

        [Test]
        public void ObjectPoolProvider_GetProvider_ReturnsCorrectProvider()
        {
            // This test would depend on your IAssemblyObjectPoolProviderFactory implementation
            // Since factories are determined by assembly attributes, this is challenging to test directly
            
            Assert.Ignore("This test requires a test assembly with provider factory attributes");
            
            /* Example of a full test with configured test assembly:
            
            // Arrange - Setup test assembly with our factory attributes
            
            // Act
            var provider = ObjectPoolProvider.GetProvider<TestItem>();
            
            // Assert
            Assert.IsNotNull(provider);
            Assert.IsInstanceOf<ExpectedProviderType>(provider);
            */
        }
    }
} 