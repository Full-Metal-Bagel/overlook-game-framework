using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Overlook.Pool.Tests
{
    // Test objects and policies for comprehensive testing
    public class ComplexPoolObject : IDisposable, IObjectPoolCallback
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDisposed { get; private set; }
        public bool WasRented { get; private set; }
        public bool WasRecycled { get; private set; }
        public int RentCount { get; private set; }
        public int RecycleCount { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public void OnRent()
        {
            WasRented = true;
            RentCount++;
        }

        public void OnRecycle()
        {
            WasRecycled = true;
            RecycleCount++;
        }

        public void Reset()
        {
            Id = 0;
            Name = null;
            WasRented = false;
            WasRecycled = false;
        }
    }

    public struct ComplexObjectPolicy : IObjectPoolPolicy
    {
        public int InitCount => 3;
        public int MaxCount => 10;
        public int Expand(int size) => size * 2;
        public object Create() => new ComplexPoolObject();
    }

    public struct CustomExpandPolicy : IObjectPoolPolicy
    {
        public int InitCount => 0;
        public int MaxCount => 20;
        public int Expand(int size) => size + 5; // Linear expansion rather than doubling
        public object Create() => new SimplePoolObject();
    }

    [TestFixture]
    public class ComprehensivePoolTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Reset static pools to ensure clean test environment
            StaticPools.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // Additional cleanup between tests if needed
        }

        [Test]
        public void ObjectPool_ExpandsCorrectly_WhenOutOfObjects()
        {
            // Arrange
            var pool = new ObjectPool<SimplePoolObject, CustomExpandPolicy>();
            Assert.That(pool.PooledCount, Is.EqualTo(0), "Pool should start empty");

            // Act - Rent one object, which should create one and expand by 5 more
            var obj1 = pool.Rent();

            // Assert
            Assert.That(obj1, Is.Not.Null);
            Assert.That(pool.RentedCount, Is.EqualTo(1));
            Assert.That(pool.PooledCount, Is.EqualTo(4), "Pool should expand by 5 according to policy");

            // Rent 5 more objects (should empty the pool and expand again)
            var rentedObjects = new List<SimplePoolObject> { obj1 };
            for (int i = 0; i < 5; i++)
            {
                rentedObjects.Add(pool.Rent());
            }

            // Assert again
            Assert.That(rentedObjects.Count, Is.EqualTo(6));
            Assert.That(pool.RentedCount, Is.EqualTo(6));
            Assert.That(pool.PooledCount, Is.EqualTo(4), "Pool should expand by 5 more");

            // Clean up
            foreach (var obj in rentedObjects)
            {
                pool.Recycle(obj);
            }
        }

        [Test]
        public void ObjectPool_WithMaxCapacity_DisposesExcessObjects()
        {
            // Arrange
            var pool = new ObjectPool<ComplexPoolObject, ComplexObjectPolicy>();
            var rentedObjects = new List<ComplexPoolObject>();

            // Act - Rent more than MaxCount objects
            for (int i = 0; i < pool.MaxCount + 5; i++)
            {
                var obj = pool.Rent();
                obj.Id = i;
                rentedObjects.Add(obj);
            }

            // Recycle all objects (some should be disposed when exceeding MaxCount)
            foreach (var obj in rentedObjects)
            {
                pool.Recycle(obj);
            }

            // Assert
            Assert.That(pool.PooledCount, Is.EqualTo(pool.MaxCount), "Pool should contain exactly MaxCount objects");

            // Rent all objects from the pool to check which ones were kept
            var recycledObjects = new List<ComplexPoolObject>();
            for (int i = 0; i < pool.MaxCount; i++)
            {
                recycledObjects.Add(pool.Rent());
            }

            // Assert that no objects are disposed in the pool
            Assert.That(recycledObjects, Is.All.Matches<ComplexPoolObject>(o => !o.IsDisposed),
                "Objects in the pool should not be disposed");
            // Verify that the remaining objects were the ones that were recycled first (FIFO behavior)
            var expectedIds = new HashSet<int>();
            for (int i = 0; i < pool.MaxCount; i++)
            {
                expectedIds.Add(rentedObjects[i].Id);
            }

            foreach (var obj in recycledObjects)
            {
                Assert.That(expectedIds, Contains.Item(obj.Id),
                    "Pool should keep the first recycled objects (FIFO behavior)");
            }

            // Clean up
            foreach (var obj in recycledObjects)
            {
                pool.Recycle(obj);
            }
        }

        [Test]
        public void ObjectPool_CallbacksInvoked_InCorrectOrder()
        {
            // Arrange
            var pool = new ObjectPool<ComplexPoolObject, ComplexObjectPolicy>();

            // Act - Rent an object and track the callback state
            var obj = pool.Rent();

            // Assert initial state
            Assert.That(obj.WasRented, Is.True, "OnRent should be called");
            Assert.That(obj.WasRecycled, Is.False, "OnRecycle should not be called yet");
            Assert.That(obj.RentCount, Is.EqualTo(1), "RentCount should be incremented");
            Assert.That(obj.RecycleCount, Is.EqualTo(0), "RecycleCount should not be incremented yet");

            // Recycle object
            pool.Recycle(obj);

            // Rent the same object again
            var obj2 = pool.Rent();
            while (!ReferenceEquals(obj, obj2))
            {
                pool.Recycle(obj2);
                obj2 = pool.Rent();
            }

            // Assert final state
            Assert.That(obj2.WasRented, Is.True, "OnRent should be called again");
            Assert.That(obj2.WasRecycled, Is.True, "OnRecycle should have been called");
            Assert.That(obj2.RentCount, Is.EqualTo(2), "RentCount should be incremented twice");
            Assert.That(obj2.RecycleCount, Is.EqualTo(1), "RecycleCount should be incremented once");

            // Clean up
            pool.Recycle(obj2);
        }

        [Test]
        public void ObjectPool_ThreadSafety_WithHighContention()
        {
            // Arrange
            const int operationsPerThread = 1000;
            const int threadCount = 8;
            var pool = new ObjectPool<ComplexPoolObject, ComplexObjectPolicy>();
            var resetEvent = new ManualResetEventSlim(false);
            var exceptions = new List<Exception>();
            var initialCount = pool.InitCount;

            // Act - Create multiple threads that will rent and recycle objects concurrently
            var threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        // Wait for all threads to be ready
                        resetEvent.Wait();

                        var threadLocalObjects = new List<ComplexPoolObject>();
                        for (int j = 0; j < operationsPerThread; j++)
                        {
                            // Rent an object
                            var obj = pool.Rent();
                            obj.Id = j;
                            threadLocalObjects.Add(obj);

                            // Randomly recycle some objects to create contention
                            if (j % 3 == 0 && threadLocalObjects.Count > 0)
                            {
                                int index = j % threadLocalObjects.Count;
                                var objToRecycle = threadLocalObjects[index];
                                threadLocalObjects.RemoveAt(index);
                                pool.Recycle(objToRecycle);
                            }
                        }

                        // Recycle all remaining objects
                        foreach (var obj in threadLocalObjects)
                        {
                            pool.Recycle(obj);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
                threads[i].Start();
            }

            // Signal all threads to start simultaneously
            resetEvent.Set();

            // Wait for all threads to complete
            foreach (var thread in threads)
            {
                thread.Join();
            }

            // Assert
            Assert.That(exceptions, Is.Empty, "No exceptions should be thrown during concurrent operations");

            // The final state should be consistent
            Assert.That(pool.RentedCount, Is.EqualTo(0), "All objects should be returned to the pool");
            Assert.That(pool.PooledCount, Is.LessThanOrEqualTo(pool.MaxCount), "Pool should not exceed MaxCount");
        }

        [Test]
        public void StaticPoolCacheMaintainsCorrectness_AcrossMultipleProviders()
        {
            // Arrange
            var provider1 = new CustomObjectPoolProvider<SimplePoolObject, EmptyInitPoolPolicy>();
            var provider2 = new CustomObjectPoolProvider<SimplePoolObject, PreloadedPoolPolicy>();

            // Act
            var cache = new TypeObjectPoolCache();
            var pool1 = cache.GetPool<SimplePoolObject>(provider1);

            // Assert initial state
            Assert.That(pool1.InitCount, Is.EqualTo(0), "Should use EmptyInitPoolPolicy");
            Assert.That(pool1.MaxCount, Is.EqualTo(10), "Should use EmptyInitPoolPolicy MaxCount");

            // Try to get a pool for the same type with a different provider
            // This should throw an exception in debug mode, but we'll test both cases
            Exception caughtException = null;
            try
            {
                cache.GetPool<SimplePoolObject>(provider2);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            #if OVERLOOK_DEBUG
            Assert.That(caughtException, Is.Not.Null, "Should throw an exception when providers don't match");
            Assert.That(caughtException, Is.InstanceOf<ProviderNotMatchException>());
            #else
            // Without debug, it should return the first provider's pool
            var pool2 = cache.GetPool<SimplePoolObject>(provider2);
            Assert.That(pool2, Is.SameAs(pool1), "Should return the same pool instance");
            #endif

            // Clean up
            cache.Dispose();
        }

        [Test]
        public void StaticPools_Interact_WithTypeObjectPoolCache()
        {
            // This test verifies the relationship between StaticPools and TypeObjectPoolCache

            // Arrange - Get a pool from StaticPools
            var pool1 = StaticPools.GetPool<SimplePoolObject>();

            // Rent and configure an object
            var obj = pool1.Rent();
            obj.Value = 42;
            pool1.Recycle(obj);

            // Clear the static pools
            StaticPools.Clear();

            // Get a new pool for the same type
            var pool2 = StaticPools.GetPool<SimplePoolObject>();

            // Assert
            Assert.That(pool2, Is.Not.SameAs(pool1), "After clearing, should get a new pool instance");

            // The new pool should be empty or pre-initialized based on the policy
            var newObj = pool2.Rent();
            Assert.That(newObj.Value, Is.EqualTo(0), "New object should have default value");

            // Clean up
            pool2.Recycle(newObj);
        }

        [Test]
        public void IObjectPool_NonGenericInterface_CorrectlyCastsObjects()
        {
            // Arrange
            IObjectPool pool = new ObjectPool<SimplePoolObject, EmptyInitPoolPolicy>();

            // Act - Rent and recycle through the non-generic interface
            var obj = pool.Rent();

            // Assert
            Assert.That(obj, Is.Not.Null);
            Assert.That(obj, Is.InstanceOf<SimplePoolObject>());

            // Try to recycle with a wrong type
            var wrongTypeObj = new DisposablePoolObject();
            Assert.Throws<InvalidCastException>(() => pool.Recycle(wrongTypeObj),
                "Should throw when recycling wrong object type");

            // Recycle properly
            pool.Recycle(obj);
            Assert.That(pool.RentedCount, Is.EqualTo(0), "Object should be recycled");
            Assert.That(pool.PooledCount, Is.EqualTo(1), "Object should be in the pool");
        }

        [Test]
        public void ObjectPool_Integration_WithMultiplePolicies()
        {
            // Test the complete object pool lifecycle with multiple policy types

            // Create different pool types
            var emptyPool = new ObjectPool<SimplePoolObject, EmptyInitPoolPolicy>();
            var preloadedPool = new ObjectPool<SimplePoolObject, PreloadedPoolPolicy>();
            var limitedPool = new ObjectPool<SimplePoolObject, LimitedPoolPolicy>();
            var complexPool = new ObjectPool<ComplexPoolObject, ComplexObjectPolicy>();

            // Verify initial state based on policies
            Assert.That(emptyPool.PooledCount, Is.EqualTo(0), "EmptyInitPoolPolicy should start with 0 objects");
            Assert.That(preloadedPool.PooledCount, Is.EqualTo(5), "PreloadedPoolPolicy should start with 5 objects");
            Assert.That(limitedPool.PooledCount, Is.EqualTo(2), "LimitedPoolPolicy should start with 2 objects");
            Assert.That(complexPool.PooledCount, Is.EqualTo(3), "ComplexObjectPolicy should start with 3 objects");

            // Rent multiple objects from each pool
            var simpleObjects = new List<SimplePoolObject>();
            var complexObjects = new List<ComplexPoolObject>();

            // From empty pool - should create new objects
            for (int i = 0; i < 5; i++)
            {
                simpleObjects.Add(emptyPool.Rent());
            }
            Assert.That(emptyPool.RentedCount, Is.EqualTo(5));

            // From preloaded - should use existing then create new
            for (int i = 0; i < 8; i++)
            {
                simpleObjects.Add(preloadedPool.Rent());
            }
            Assert.That(preloadedPool.RentedCount, Is.EqualTo(8));
            Assert.That(preloadedPool.PooledCount, Is.EqualTo(1), "All objects should be rented");

            // From limited - test the max capacity
            for (int i = 0; i < limitedPool.MaxCount + 3; i++)
            {
                simpleObjects.Add(limitedPool.Rent());
            }
            Assert.That(limitedPool.RentedCount, Is.EqualTo(limitedPool.MaxCount + 3));

            // From complex - test with mixed objects
            for (int i = 0; i < 10; i++)
            {
                var obj = complexPool.Rent();
                obj.Id = i;
                obj.Name = $"Object {i}";
                complexObjects.Add(obj);
            }

            // Recycle all objects
            foreach (var obj in simpleObjects)
            {
                if (simpleObjects.IndexOf(obj) < 5)
                {
                    emptyPool.Recycle(obj);
                }
                else if (simpleObjects.IndexOf(obj) < 13)
                {
                    preloadedPool.Recycle(obj);
                }
                else
                {
                    limitedPool.Recycle(obj);
                }
            }

            foreach (var obj in complexObjects)
            {
                complexPool.Recycle(obj);
            }

            // Verify final state
            Assert.That(emptyPool.RentedCount, Is.EqualTo(0));
            Assert.That(emptyPool.PooledCount, Is.EqualTo(5), "All objects should be in pool up to MaxCount");

            Assert.That(preloadedPool.RentedCount, Is.EqualTo(0));
            Assert.That(preloadedPool.PooledCount, Is.EqualTo(9), "All recycled objects should be in pool");

            Assert.That(limitedPool.RentedCount, Is.EqualTo(0));
            Assert.That(limitedPool.PooledCount, Is.EqualTo(limitedPool.MaxCount),
                "Pool should be limited to MaxCount objects");

            Assert.That(complexPool.RentedCount, Is.EqualTo(0));
            Assert.That(complexPool.PooledCount, Is.EqualTo(complexPool.MaxCount),
                "Complex pool should be limited to MaxCount objects");

            // Dispose all pools
            emptyPool.Dispose();
            preloadedPool.Dispose();
            limitedPool.Dispose();
            complexPool.Dispose();

            // For complex pool with disposable objects, verify they are disposed
            Assert.That(complexObjects, Is.All.Matches<ComplexPoolObject>(o => o.IsDisposed),
                "All complex objects should be disposed when pool is disposed");
        }

        [Test]
        public void PooledCollections_DefaultConstructors_WorkCorrectly()
        {
            // Test all the default constructors added in the latest updates

            using (var list = new PooledList<int>())
            {
                Assert.That(list.Value, Is.Not.Null);
                Assert.That(list.Value.Count, Is.EqualTo(0));

                // Add data to confirm it works
                list.Value.Add(1);
                list.Value.Add(2);
                Assert.That(list.Value.Count, Is.EqualTo(2));
            }

            using (var dict = new PooledDictionary<string, int>())
            {
                Assert.That(dict.Value, Is.Not.Null);
                Assert.That(dict.Value.Count, Is.EqualTo(0));

                // Add data to confirm it works
                dict.Value["test"] = 42;
                Assert.That(dict.Value.Count, Is.EqualTo(1));
                Assert.That(dict.Value["test"], Is.EqualTo(42));
            }

            using (var hashSet = new PooledHashSet<int>())
            {
                Assert.That(hashSet.Value, Is.Not.Null);
                Assert.That(hashSet.Value.Count, Is.EqualTo(0));

                // Add data to confirm it works
                hashSet.Value.Add(1);
                hashSet.Value.Add(2);
                Assert.That(hashSet.Value.Count, Is.EqualTo(2));
                Assert.That(hashSet.Value.Contains(1), Is.True);
            }

            using (var stringBuilder = new PooledStringBuilder())
            {
                Assert.That(stringBuilder.Value, Is.Not.Null);
                Assert.That(stringBuilder.Value.Length, Is.EqualTo(0));

                // Add data to confirm it works
                stringBuilder.Value.Append("Hello");
                Assert.That(stringBuilder.Value.ToString(), Is.EqualTo("Hello"));
            }

            using (var memList = new PooledMemoryList<byte>())
            {
                Assert.That(memList.Value, Is.Not.Null);
                Assert.That(memList.Value.Count, Is.EqualTo(0));

                // Add data to confirm it works
                memList.Value.Add(1);
                memList.Value.Add(2);
                Assert.That(memList.Value.Count, Is.EqualTo(2));
                Assert.That(memList.Value[0], Is.EqualTo(1));
            }

            using (var memDict = new PooledMemoryDictionary<int, string>())
            {
                Assert.That(memDict.Value, Is.Not.Null);
                Assert.That(memDict.Value.Count, Is.EqualTo(0));

                // Add data to confirm it works
                memDict.Value[1] = "test";
                Assert.That(memDict.Value.Count, Is.EqualTo(1));
                Assert.That(memDict.Value[1], Is.EqualTo("test"));
            }
        }

        [Test]
        public void PooledCollections_CompareDefaultAndCapacityCtors()
        {
            // Test comparing the default and capacity constructors

            // List
            using (var defaultList = new PooledList<int>())
            using (var capacityList = new PooledList<int>(10))
            {
                Assert.That(defaultList.Value.Count, Is.EqualTo(0));
                Assert.That(capacityList.Value.Count, Is.EqualTo(0));
                Assert.That(capacityList.Value.Capacity, Is.GreaterThanOrEqualTo(10));

                // Both should be usable
                defaultList.Value.Add(1);
                capacityList.Value.Add(2);

                Assert.That(defaultList.Value[0], Is.EqualTo(1));
                Assert.That(capacityList.Value[0], Is.EqualTo(2));
            }

            // Dictionary
            using (var defaultDict = new PooledDictionary<int, string>())
            using (var capacityDict = new PooledDictionary<int, string>(10))
            {
                Assert.That(defaultDict.Value.Count, Is.EqualTo(0));
                Assert.That(capacityDict.Value.Count, Is.EqualTo(0));

                // Both should be usable
                defaultDict.Value[1] = "default";
                capacityDict.Value[2] = "capacity";

                Assert.That(defaultDict.Value[1], Is.EqualTo("default"));
                Assert.That(capacityDict.Value[2], Is.EqualTo("capacity"));
            }

            // HashSet
            using (var defaultSet = new PooledHashSet<int>())
            using (var capacitySet = new PooledHashSet<int>(10))
            {
                Assert.That(defaultSet.Value.Count, Is.EqualTo(0));
                Assert.That(capacitySet.Value.Count, Is.EqualTo(0));

                // Both should be usable
                defaultSet.Value.Add(1);
                capacitySet.Value.Add(2);

                Assert.That(defaultSet.Value.Contains(1), Is.True);
                Assert.That(capacitySet.Value.Contains(2), Is.True);
            }

            // StringBuilder
            using (var defaultSb = new PooledStringBuilder())
            using (var capacitySb = new PooledStringBuilder(10))
            {
                Assert.That(defaultSb.Value.Length, Is.EqualTo(0));
                Assert.That(capacitySb.Value.Length, Is.EqualTo(0));
                Assert.That(capacitySb.Value.Capacity, Is.GreaterThanOrEqualTo(10));

                // Both should be usable
                defaultSb.Value.Append("default");
                capacitySb.Value.Append("capacity");

                Assert.That(defaultSb.Value.ToString(), Is.EqualTo("default"));
                Assert.That(capacitySb.Value.ToString(), Is.EqualTo("capacity"));
            }

            // MemoryList
            using (var defaultMemList = new PooledMemoryList<byte>())
            using (var capacityMemList = new PooledMemoryList<byte>(10))
            {
                Assert.That(defaultMemList.Value.Count, Is.EqualTo(0));
                Assert.That(capacityMemList.Value.Count, Is.EqualTo(0));
                Assert.That(capacityMemList.Value.Capacity, Is.GreaterThanOrEqualTo(10));

                // Both should be usable
                defaultMemList.Value.Add(1);
                capacityMemList.Value.Add(2);

                Assert.That(defaultMemList.Value[0], Is.EqualTo(1));
                Assert.That(capacityMemList.Value[0], Is.EqualTo(2));
            }

            // MemoryDictionary
            using (var defaultMemDict = new PooledMemoryDictionary<int, string>())
            using (var capacityMemDict = new PooledMemoryDictionary<int, string>(10))
            {
                Assert.That(defaultMemDict.Value.Count, Is.EqualTo(0));
                Assert.That(capacityMemDict.Value.Count, Is.EqualTo(0));

                // Both should be usable
                defaultMemDict.Value[1] = "default";
                capacityMemDict.Value[2] = "capacity";

                Assert.That(defaultMemDict.Value[1], Is.EqualTo("default"));
                Assert.That(capacityMemDict.Value[2], Is.EqualTo("capacity"));
            }
        }
    }
}
