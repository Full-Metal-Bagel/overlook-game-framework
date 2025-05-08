using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

[assembly: Overlook.Pool.OverrideGenericCollectionPoolPolicy(typeof(Overlook.Pool.Tests.GenericCollection<>), typeof(Overlook.Pool.Tests.CustomGenericPolicy))]

namespace Overlook.Pool.Tests
{
    public class CustomPooledType { }
    public class GenericCollection<T> : List<T>, ICollection<T> { }

    public struct CustomGenericPolicy : IObjectPoolPolicy
    {
        public int InitCount => 3;
        public int MaxCount => 15;
        public int Expand(int size) => size + 5;
        public object Create() => new GenericCollection<int>();
    }

    public class CustomObjectPoolProviderFactory : IObjectPoolProviderFactory
    {
        public IObjectPoolProvider CreateProvider(Type type)
        {
            return type == typeof(CustomPooledType)
                ? new CustomObjectPoolProvider<CustomPooledType, EmptyInitPoolPolicy>()
                : null;
        }
    }

    [TestFixture]
    public class PoolProviderTests
    {
        [Test]
        public void RegisterAttributes_AreCorrectlyApplied()
        {
            // Test for OverridePoolPolicy attribute
            var provider = ObjectPoolProvider.Get<TestObjectB>();
            var pool = provider.CreatePool();

            Assert.That(pool, Is.InstanceOf<ObjectPool<TestObjectB, CustomTestPoolPolicy>>());
            Assert.That(pool.InitCount, Is.EqualTo(7), "Should use CustomTestPoolPolicy's InitCount");
            Assert.That(pool.MaxCount, Is.EqualTo(25), "Should use CustomTestPoolPolicy's MaxCount");
        }

        [Test]
        public void GenericCollectionPoolPolicy_WorksWithRegisteredTypes()
        {
            // Test for OverrideGenericCollectionPoolPolicy attribute
            var provider = ObjectPoolProvider.Get<GenericCollection<int>>();
            var pool = provider.CreatePool();

            Assert.That(pool, Is.Not.Null);
            Assert.That(pool.InitCount, Is.EqualTo(3), "Should use CustomGenericPolicy's InitCount");
            Assert.That(pool.MaxCount, Is.EqualTo(15), "Should use CustomGenericPolicy's MaxCount");

            // Verify the pool works correctly
            var collection = (GenericCollection<int>)pool.Rent();
            Assert.That(collection, Is.Not.Null);
            Assert.That(collection, Is.InstanceOf<GenericCollection<int>>());

            collection.Add(42);
            collection.Add(100);

            pool.Recycle(collection);

            // The recycled object should have been cleared (if ICollection policy is working)
            var recycled = (GenericCollection<int>)pool.Rent();
            Assert.That(recycled.Count, Is.EqualTo(0), "Collection should be cleared after recycling");

            pool.Recycle(recycled);
        }

        [Test]
        public void DefaultObjectPoolProvider_CreatesCorrectPool()
        {
            var provider = new DefaultObjectPoolProvider<CustomPooledType>();
            var pool = provider.CreatePool();

            Assert.That(pool, Is.Not.Null);
            Assert.That(pool, Is.InstanceOf<ObjectPool<CustomPooledType, DefaultObjectPoolPolicy<CustomPooledType>>>());

            // Verify default policy values
            var policy = (IObjectPoolPolicy)new DefaultObjectPoolPolicy<CustomPooledType>();
            var initCount = policy.InitCount;;
            var maxCount = policy.MaxCount;;

            Assert.That(pool.InitCount, Is.EqualTo(initCount));
            Assert.That(pool.MaxCount, Is.EqualTo(maxCount));
        }

        [Test]
        public void CustomObjectPoolProvider_CreatesCorrectPool()
        {
            var provider = new CustomObjectPoolProvider<SimplePoolObject, EmptyInitPoolPolicy>();
            var pool = provider.CreatePool();

            Assert.That(pool, Is.Not.Null);
            Assert.That(pool, Is.InstanceOf<ObjectPool<SimplePoolObject, EmptyInitPoolPolicy>>());
            Assert.That(pool.InitCount, Is.EqualTo(0), "Should use EmptyInitPoolPolicy's InitCount");
            Assert.That(pool.MaxCount, Is.EqualTo(10), "Should use EmptyInitPoolPolicy's MaxCount");
        }

        [Test]
        public void DefaultCollectionPoolProvider_WorksWithGenericCollections()
        {
            // This test verifies that the default collection pool provider works
            var provider = ObjectPoolProvider.Get<List<string>>();
            var pool = provider.CreatePool();

            Assert.That(pool, Is.Not.Null);

            // Verify the pool creates List<string> objects
            var list = pool.Rent();
            Assert.That(list, Is.InstanceOf<List<string>>());

            // Cast and use the list
            var typedList = (List<string>)list;
            typedList.Add("test");
            typedList.Add("object");
            Assert.That(typedList.Count, Is.EqualTo(2));

            // Recycle and verify clearing behavior
            pool.Recycle(list);

            var recycledList = (List<string>)pool.Rent();
            Assert.That(recycledList.Count, Is.EqualTo(0), "List should be cleared after recycling");

            pool.Recycle(recycledList);
        }

        private class MockAssemblyFactory : IAssemblyObjectPoolProviderFactory
        {
            private readonly int _priority;
            private readonly bool _returnProvider;

            public bool WasCalled { get; private set; }

            public MockAssemblyFactory(int priority, bool returnProvider)
            {
                _priority = priority;
                _returnProvider = returnProvider;
            }

            public int Priority => _priority;

            public IObjectPoolProvider CreateProvider(Type type)
            {
                WasCalled = true;
                return _returnProvider ? new DefaultObjectPoolProvider<CustomPooledType>() : null;
            }
        }
    }
}
