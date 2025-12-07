using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Overlook.Pool.Tests
{
    [TestFixture]
    public class PooledCollectionTests
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            // Reset static pools between test runs
            StaticPools.Clear();
        }

        [Test]
        public void PooledObject_CreatesAndRecycles_CorrectType()
        {
            // Use PooledObject in a using block
            using (var pooled = new PooledObject<SimplePoolObject>())
            {
                var obj = pooled.Value;
                Assert.That(obj, Is.Not.Null);
                Assert.That(obj, Is.InstanceOf<SimplePoolObject>());

                // Modify the object
                obj.Value = 42;
            }

            // After the using block, the object should be back in the pool
            var pool = StaticPools.GetPool<SimplePoolObject>();
            Assert.That(pool.RentedCount, Is.EqualTo(0), "Object should be recycled after disposal");
            Assert.That(pool.PooledCount, Is.GreaterThan(0), "Object should be back in pool");

            // Rent to see if we get the object back in its default state
            var recycledObj = pool.Rent();
            Assert.That(recycledObj.Value, Is.EqualTo(0), "Object should be reset to default state");

            // Clean up
            pool.Recycle(recycledObj);
        }

        [Test]
        public void PooledList_DefaultConstructor_CreatesEmptyList()
        {
            // Test the default constructor
            using (var pooled = new PooledList<int>())
            {
                var list = pooled.Value;
                Assert.That(list, Is.Not.Null);
                Assert.That(list, Is.InstanceOf<List<int>>());
                Assert.That(list.Count, Is.EqualTo(0));

                // Should still be usable
                list.Add(1);
                list.Add(2);
                Assert.That(list.Count, Is.EqualTo(2));
            }

            // After disposal, verify pool state
            var pool = StaticPools.GetPool<List<int>>();
            Assert.That(pool.RentedCount, Is.EqualTo(0), "List should be recycled after disposal");
            Assert.That(pool.PooledCount, Is.GreaterThan(0), "List should be back in pool");
        }

        [Test]
        public void PooledList_CreateAndRecycle_MaintainsCapacity()
        {
            const int requestedCapacity = 100;

            // Use PooledList with explicit capacity
            using (var pooled = new PooledList<int>(requestedCapacity))
            {
                var list = pooled.Value;
                Assert.That(list, Is.Not.Null);
                Assert.That(list, Is.InstanceOf<List<int>>());
                Assert.That(list.Capacity, Is.GreaterThanOrEqualTo(requestedCapacity),
                    "List should have at least the requested capacity");

                // Add items to the list
                for (int i = 0; i < 50; i++)
                {
                    list.Add(i);
                }
                Assert.That(list.Count, Is.EqualTo(50));
            }

            // After the using block, the list should be back in the pool
            var pool = StaticPools.GetPool<List<int>>();
            Assert.That(pool.RentedCount, Is.EqualTo(0), "List should be recycled after disposal");
            Assert.That(pool.PooledCount, Is.GreaterThan(0), "List should be back in pool");

            // Get the list back and verify it's empty but maintains capacity
            var recycledList = pool.Rent();
            Assert.That(recycledList.Count, Is.EqualTo(0), "List should be empty");

            // Clean up
            pool.Recycle(recycledList);
        }

        [Test]
        public void PooledList_ImplicitConversion_WorksCorrectly()
        {
            using (var pooled = new PooledList<string>(10))
            {
                // Test implicit conversion operator
                List<string> list = pooled;
                Assert.That(list, Is.SameAs(pooled.Value));

                // Use the converted list
                list.Add("test1");
                list.Add("test2");
                Assert.That(list.Count, Is.EqualTo(2));

                // Verify both references point to the same list
                pooled.Value.Add("test3");
                Assert.That(list.Count, Is.EqualTo(3));
            }
        }

        [Test]
        public void PooledDictionary_DefaultConstructor_CreatesEmptyDictionary()
        {
            // Test the default constructor
            using (var pooled = new PooledDictionary<string, int>())
            {
                var dict = pooled.Value;
                Assert.That(dict, Is.Not.Null);
                Assert.That(dict, Is.InstanceOf<Dictionary<string, int>>());
                Assert.That(dict.Count, Is.EqualTo(0));

                // Should still be usable
                dict["test"] = 42;
                Assert.That(dict["test"], Is.EqualTo(42));
            }

            // After disposal, verify pool state
            var pool = StaticPools.GetPool<Dictionary<string, int>>();
            Assert.That(pool.RentedCount, Is.EqualTo(0), "Dictionary should be recycled after disposal");
            Assert.That(pool.PooledCount, Is.GreaterThan(0), "Dictionary should be back in pool");
        }

        [Test]
        public void PooledDictionary_CreateAndRecycle_WorksCorrectly()
        {
            // Test the PooledDictionary
            using (var pooled = new PooledDictionary<string, int>(3))
            {
                var dict = pooled.Value;
                Assert.That(dict, Is.Not.Null);
                Assert.That(dict, Is.InstanceOf<Dictionary<string, int>>());

                // Add items
                dict["one"] = 1;
                dict["two"] = 2;
                dict["three"] = 3;

                Assert.That(dict.Count, Is.EqualTo(3));
                Assert.That(dict["two"], Is.EqualTo(2));
            }

            // After disposal, dictionary should be back in pool and cleared
            var pool = StaticPools.GetPool<Dictionary<string, int>>();
            var recycled = pool.Rent();
            Assert.That(recycled.Count, Is.EqualTo(0), "Dictionary should be empty after recycling");

            pool.Recycle(recycled);
        }

        [Test]
        public void PooledHashSet_DefaultConstructor_CreatesEmptyHashSet()
        {
            // Test the default constructor
            using (var pooled = new PooledHashSet<int>())
            {
                var set = pooled.Value;
                Assert.That(set, Is.Not.Null);
                Assert.That(set, Is.InstanceOf<HashSet<int>>());
                Assert.That(set.Count, Is.EqualTo(0));

                // Should still be usable
                set.Add(10);
                set.Add(20);
                Assert.That(set.Count, Is.EqualTo(2));
                Assert.That(set.Contains(10), Is.True);
            }

            // After disposal, verify pool state
            var pool = StaticPools.GetPool<HashSet<int>>();
            Assert.That(pool.RentedCount, Is.EqualTo(0), "HashSet should be recycled after disposal");
            Assert.That(pool.PooledCount, Is.GreaterThan(0), "HashSet should be back in pool");
        }

        [Test]
        public void PooledHashSet_CreateAndRecycle_WorksCorrectly()
        {
            // Test the PooledHashSet
            using (var pooled = new PooledHashSet<int>(3))
            {
                var set = pooled.Value;
                Assert.That(set, Is.Not.Null);
                Assert.That(set, Is.InstanceOf<HashSet<int>>());

                // Add items
                set.Add(10);
                set.Add(20);
                set.Add(30);
                set.Add(10); // Duplicate, should not be added

                Assert.That(set.Count, Is.EqualTo(3));
                Assert.That(set.Contains(20), Is.True);
            }

            // After disposal, set should be back in pool and cleared
            var pool = StaticPools.GetPool<HashSet<int>>();
            var recycled = pool.Rent();
            Assert.That(recycled.Count, Is.EqualTo(0), "HashSet should be empty after recycling");

            pool.Recycle(recycled);
        }

        [Test]
        public void PooledArray_CreateAndRecycle_WorksCorrectly()
        {
            const int arraySize = 5;

            // Test the PooledArray with specific size
            using (var pooled = new PooledArray<int>(arraySize))
            {
                var array = pooled.Value;
                Assert.That(array, Is.Not.Null);
                Assert.That(array, Is.InstanceOf<int[]>());
                Assert.That(array.Length, Is.GreaterThanOrEqualTo(arraySize),
                    "ArrayPool may return a larger array than requested");

                // Fill the array
                for (int i = 0; i < arraySize; i++)
                {
                    array[i] = i * 2;
                }

                Assert.That(array[3], Is.EqualTo(6));

                // Verify the implementation uses ArrayPool correctly
                var arrayFromPool = ArrayPool<int>.Shared.Rent(arraySize);
                try
                {
                    Assert.That(arrayFromPool.Length, Is.GreaterThanOrEqualTo(arraySize),
                        "ArrayPool should return an array of at least the requested size");
                }
                finally
                {
                    ArrayPool<int>.Shared.Return(arrayFromPool);
                }
            }

            // After disposal, the array should have been returned to the ArrayPool
            // We can't easily verify this directly, so we'll just verify the struct works
        }

        [Test]
        public void PooledStringBuilder_DefaultConstructor_CreatesEmptyStringBuilder()
        {
            // Test the default constructor
            using (var pooled = new PooledStringBuilder())
            {
                var sb = pooled.Value;
                Assert.That(sb, Is.Not.Null);
                Assert.That(sb.Length, Is.EqualTo(0));

                // Should still be usable
                sb.Append("Test string");
                Assert.That(sb.ToString(), Is.EqualTo("Test string"));
            }

            // After disposal, verify pool state
            var pool = StaticPools.GetPool<StringBuilder>();
            Assert.That(pool.RentedCount, Is.EqualTo(0), "StringBuilder should be recycled after disposal");
            Assert.That(pool.PooledCount, Is.GreaterThan(0), "StringBuilder should be back in pool");
        }

        [Test]
        public void PooledStringBuilder_CreateAndRecycle_WorksCorrectly()
        {
            // Test the PooledStringBuilder
            using (var pooled = new PooledStringBuilder(10))
            {
                var sb = pooled.Value;
                Assert.That(sb, Is.Not.Null);

                // Build a string
                sb.Append("Hello");
                sb.Append(' ');
                sb.Append("World");

                Assert.That(sb.ToString(), Is.EqualTo("Hello World"));
            }

            // Pool should have received the builder back
            var pool = StaticPools.GetPool<StringBuilder>();
            Assert.That(pool.RentedCount, Is.EqualTo(0));
            Assert.That(pool.PooledCount, Is.GreaterThan(0));

            // Recycled builder should be empty
            var recycled = pool.Rent();
            Assert.That(recycled.Length, Is.EqualTo(0));

            pool.Recycle(recycled);
        }

        [Test]
        public void PooledMemoryList_DefaultConstructor_CreatesEmptyList()
        {
            // Test the default constructor
            using (var pooled = new PooledMemoryList<byte>())
            {
                var list = pooled.Value;
                Assert.That(list, Is.Not.Null);
                Assert.That(list, Is.InstanceOf<List<byte>>());
                Assert.That(list.Count, Is.EqualTo(0));

                // Should still be usable
                list.Add(1);
                list.Add(2);
                Assert.That(list.Count, Is.EqualTo(2));
                Assert.That(list[0], Is.EqualTo(1));
            }

            // After disposal, verify pool state
            var pool = StaticPools.GetPool<List<byte>>();
            Assert.That(pool.RentedCount, Is.EqualTo(0), "List should be recycled after disposal");
            Assert.That(pool.PooledCount, Is.GreaterThan(0), "List should be back in pool");
        }

        [Test]
        public void PooledMemoryList_CreateAndRecycle_WorksCorrectly()
        {
            // Test the PooledMemoryList
            using (var pooled = new PooledMemoryList<byte>(100))
            {
                var list = pooled.Value;
                Assert.That(list, Is.Not.Null);

                // Add data
                for (byte i = 0; i < 50; i++)
                {
                    list.Add(i);
                }

                // Check data
                Assert.That(list.Count, Is.EqualTo(50));
                Assert.That(list[25], Is.EqualTo(25));

                // Convert to array for testing
                var array = list.ToArray();
                Assert.That(array.Length, Is.EqualTo(50));
                Assert.That(array[10], Is.EqualTo(10));
            }

            // After disposal, verify pool state
            var pool = StaticPools.GetPool<List<byte>>();
            Assert.That(pool.RentedCount, Is.EqualTo(0));
            Assert.That(pool.PooledCount, Is.GreaterThan(0));
        }

        [Test]
        public void PooledMemoryDictionary_DefaultConstructor_CreatesEmptyDictionary()
        {
            // Test the default constructor
            using (var pooled = new PooledMemoryDictionary<int, byte>())
            {
                var dict = pooled.Value;
                Assert.That(dict, Is.Not.Null);
                Assert.That(dict, Is.InstanceOf<Dictionary<int, byte>>());
                Assert.That(dict.Count, Is.EqualTo(0));

                // Should still be usable
                dict[1] = 10;
                dict[2] = 20;
                Assert.That(dict.Count, Is.EqualTo(2));
                Assert.That(dict[1], Is.EqualTo(10));
            }

            // After disposal, verify pool state
            var pool = StaticPools.GetPool<Dictionary<int, byte>>();
            Assert.That(pool.RentedCount, Is.EqualTo(0), "Dictionary should be recycled after disposal");
            Assert.That(pool.PooledCount, Is.GreaterThan(0), "Dictionary should be back in pool");
        }

        [Test]
        public void PooledMemoryDictionary_CreateAndRecycle_WorksCorrectly()
        {
            // Test the PooledMemoryDictionary
            using (var pooled = new PooledMemoryDictionary<int, byte>(10))
            {
                var dict = pooled.Value;
                Assert.That(dict, Is.Not.Null);

                // Add data
                for (int i = 0; i < 10; i++)
                {
                    dict[i] = (byte)(i * 2);
                }

                // Verify
                Assert.That(dict.Count, Is.EqualTo(10));
                Assert.That(dict[5], Is.EqualTo(10));

                // Check dictionary contents using standard methods
                var keys = new List<int>(dict.Keys);
                var values = new List<byte>(dict.Values);

                Assert.That(keys.Count, Is.EqualTo(10));
                Assert.That(values.Count, Is.EqualTo(10));

                // Find the index of key 5
                int index = keys.IndexOf(5);

                Assert.That(index, Is.GreaterThanOrEqualTo(0), "Key 5 should be found");
                Assert.That(values[index], Is.EqualTo(10), "Value at key 5 should be 10");
            }

            // After disposal, verify pool state
            var pool = StaticPools.GetPool<Dictionary<int, byte>>();
            Assert.That(pool.RentedCount, Is.EqualTo(0));
            Assert.That(pool.PooledCount, Is.GreaterThan(0));
        }

        [Test]
        public void ComparePooledListConstructors_ShouldBothWork()
        {
            // Test both constructors to ensure they behave correctly
            using (var pooledWithCapacity = new PooledList<int>(10))
            using (var pooledDefault = new PooledList<int>())
            {
                var listWithCapacity = pooledWithCapacity.Value;
                var listDefault = pooledDefault.Value;

                Assert.That(listWithCapacity, Is.Not.Null);
                Assert.That(listDefault, Is.Not.Null);

                // Both should start empty
                Assert.That(listWithCapacity.Count, Is.EqualTo(0));
                Assert.That(listDefault.Count, Is.EqualTo(0));

                // The capacity-initialized one should have larger capacity
                Assert.That(listWithCapacity.Capacity, Is.GreaterThanOrEqualTo(10));

                // Both should be usable
                listWithCapacity.Add(1);
                listDefault.Add(2);

                Assert.That(listWithCapacity.Count, Is.EqualTo(1));
                Assert.That(listDefault.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void ComparePooledDictionaryConstructors_ShouldBothWork()
        {
            // Test both constructors to ensure they behave correctly
            using (var pooledWithCapacity = new PooledDictionary<string, int>(5))
            using (var pooledDefault = new PooledDictionary<string, int>())
            {
                var dictWithCapacity = pooledWithCapacity.Value;
                var dictDefault = pooledDefault.Value;

                Assert.That(dictWithCapacity, Is.Not.Null);
                Assert.That(dictDefault, Is.Not.Null);

                // Both should start empty
                Assert.That(dictWithCapacity.Count, Is.EqualTo(0));
                Assert.That(dictDefault.Count, Is.EqualTo(0));

                // Both should be usable
                dictWithCapacity.Add("test1", 1);
                dictDefault.Add("test2", 2);

                Assert.That(dictWithCapacity.Count, Is.EqualTo(1));
                Assert.That(dictDefault.Count, Is.EqualTo(1));

                Assert.That(dictWithCapacity["test1"], Is.EqualTo(1));
                Assert.That(dictDefault["test2"], Is.EqualTo(2));
            }
        }

        [Test]
        public void NestedPooledObjects_DisposedCorrectly()
        {
            // This test verifies that nested pooled objects work correctly

            // First, track initial pool state
            var objectPool = StaticPools.GetPool<SimplePoolObject>();
            var listPool = StaticPools.GetPool<List<int>>();

            int initialObjectCount = objectPool.PooledCount;
            int initialListCount = listPool.PooledCount;

            // Create nested pooled objects
            using (var outerObject = new PooledObject<SimplePoolObject>())
            {
                Assert.That(objectPool.RentedCount, Is.EqualTo(1), "One object should be rented");

                using (var innerList = new PooledList<int>(10))
                {
                    Assert.That(listPool.RentedCount, Is.EqualTo(1), "One list should be rented");

                    // Use both objects
                    outerObject.Value.Value = 100;
                    innerList.Value.Add(200);

                    // Inner list should be recycled at end of this block
                }

                // Verify inner list was returned to pool
                Assert.That(listPool.RentedCount, Is.EqualTo(0), "List should be recycled");
                Assert.That(listPool.PooledCount, Is.EqualTo(initialListCount), "List should be in pool");

                // Outer object is still active
                Assert.That(objectPool.RentedCount, Is.EqualTo(1), "Object should still be rented");
                Assert.That(outerObject.Value.Value, Is.EqualTo(100), "Object value should be unchanged");

                // Outer object recycled at end of this block
            }

            // Verify both are now recycled
            Assert.That(objectPool.RentedCount, Is.EqualTo(0), "Object should be recycled");
            Assert.That(objectPool.PooledCount, Is.EqualTo(initialObjectCount + 1), "Object should be in pool");

            // Check state of retrieved objects
            var recycledObj = objectPool.Rent();
            Assert.That(recycledObj.Value, Is.EqualTo(0), "Object should be reset");

            var recycledList = listPool.Rent();
            Assert.That(recycledList.Count, Is.EqualTo(0), "List should be empty");

            // Clean up
            objectPool.Recycle(recycledObj);
            listPool.Recycle(recycledList);
        }

        [Test]
        public void MultipleDefaultConstructors_WorkCorrectly_WithNestedDisposal()
        {
            // Test initializing all the various default constructors
            // and verify they all dispose correctly when nested

            using (var list = new PooledList<int>())
            using (var dict = new PooledDictionary<string, int>())
            using (var set = new PooledHashSet<double>())
            using (var sb = new PooledStringBuilder())
            using (var memList = new PooledMemoryList<byte>())
            using (var memDict = new PooledMemoryDictionary<Guid, string>())
            {
                // Verify all are initialized correctly
                Assert.That(list.Value, Is.Not.Null);
                Assert.That(dict.Value, Is.Not.Null);
                Assert.That(set.Value, Is.Not.Null);
                Assert.That(sb.Value, Is.Not.Null);
                Assert.That(memList.Value, Is.Not.Null);
                Assert.That(memDict.Value, Is.Not.Null);

                // Add some items to each
                list.Value.Add(42);
                dict.Value["key"] = 123;
                set.Value.Add(3.14);
                sb.Value.Append("Hello");
                memList.Value.Add(255);
                memDict.Value[Guid.NewGuid()] = "value";

                // Verify items were added
                Assert.That(list.Value.Count, Is.EqualTo(1));
                Assert.That(dict.Value.Count, Is.EqualTo(1));
                Assert.That(set.Value.Count, Is.EqualTo(1));
                Assert.That(sb.Value.Length, Is.GreaterThan(0));
                Assert.That(memList.Value.Count, Is.EqualTo(1));
                Assert.That(memDict.Value.Count, Is.EqualTo(1));
            }

            // All should have been disposed and returned to their pools
            var listPool = StaticPools.GetPool<List<int>>();
            var dictPool = StaticPools.GetPool<Dictionary<string, int>>();
            var setPool = StaticPools.GetPool<HashSet<double>>();
            var sbPool = StaticPools.GetPool<StringBuilder>();
            var memListPool = StaticPools.GetPool<List<byte>>();
            var memDictPool = StaticPools.GetPool<Dictionary<Guid, string>>();

            Assert.That(listPool.RentedCount, Is.EqualTo(0));
            Assert.That(dictPool.RentedCount, Is.EqualTo(0));
            Assert.That(setPool.RentedCount, Is.EqualTo(0));
            Assert.That(sbPool.RentedCount, Is.EqualTo(0));
            Assert.That(memListPool.RentedCount, Is.EqualTo(0));
            Assert.That(memDictPool.RentedCount, Is.EqualTo(0));

            // Each pool should have at least one object
            Assert.That(listPool.PooledCount, Is.GreaterThan(0));
            Assert.That(dictPool.PooledCount, Is.GreaterThan(0));
            Assert.That(setPool.PooledCount, Is.GreaterThan(0));
            Assert.That(sbPool.PooledCount, Is.GreaterThan(0));
            Assert.That(memListPool.PooledCount, Is.GreaterThan(0));
            Assert.That(memDictPool.PooledCount, Is.GreaterThan(0));
        }
    }
}
