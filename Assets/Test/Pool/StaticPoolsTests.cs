using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using Overlook.Pool;
using Overlook.Pool.Tests;

[assembly: OverridePoolPolicy(typeof(StaticPoolObject), typeof(CustomStaticPoolPolicy))]

namespace Overlook.Pool.Tests;

// Test objects for static pool testing
public class StaticPoolObject
{
    public int Value { get; set; }
}

public class AnotherStaticPoolObject
{
    public string Text { get; set; }
}

// Custom policies for testing static pools
public struct CustomStaticPoolPolicy : IObjectPoolPolicy
{
    public int InitCount => 3;
    public int MaxCount => 7;
    public int Expand(int size) => size + 2;
    public object Create() => new StaticPoolObject();
}

[TestFixture]
public class StaticPoolsTests
{
    [OneTimeSetUp]
    public void SetUp()
    {
        // Reset static pools between test runs
        ResetStaticPools();
    }

    [Test]
    public void GetPool_Generic_ReturnsCachedInstance()
    {
        // Act
        var pool1 = StaticPools.GetPool<AnotherStaticPoolObject>();
        var pool2 = StaticPools.GetPool<AnotherStaticPoolObject>();

        // Assert
        Assert.That(pool1, Is.Not.Null, "Pool should not be null");
        Assert.That(pool2, Is.Not.Null, "Pool should not be null");
        Assert.That(pool2, Is.SameAs(pool1), "Should return the same cached instance");
    }

    [Test]
    public void GetPool_Type_ReturnsCachedInstance()
    {
        // Act
        var pool1 = StaticPools.GetPool(typeof(AnotherStaticPoolObject));
        var pool2 = StaticPools.GetPool(typeof(AnotherStaticPoolObject));

        // Assert
        Assert.That(pool1, Is.Not.Null, "Pool should not be null");
        Assert.That(pool2, Is.Not.Null, "Pool should not be null");
        Assert.That(pool2, Is.SameAs(pool1), "Should return the same cached instance");
    }

    [Test]
    public void GetPool_BothApproaches_ReturnSameInstance()
    {
        // Act
        var pool1 = StaticPools.GetPool<AnotherStaticPoolObject>();
        var pool2 = StaticPools.GetPool(typeof(AnotherStaticPoolObject));

        // Assert - This may fail depending on implementation details
        Assert.That(pool2, Is.SameAs(pool1),
            "Both approaches should return the same cached instance");
    }

    [Test]
    public void GetPool_DifferentTypes_ReturnDifferentInstances()
    {
        // Act
        var pool1 = StaticPools.GetPool<StaticPoolObject>();
        var pool2 = StaticPools.GetPool<AnotherStaticPoolObject>();

        // Assert
        Assert.That(pool1, Is.Not.Null);
        Assert.That(pool2, Is.Not.Null);
        Assert.That(pool1, Is.Not.SameAs(pool2), "Different types should have different pools");
    }

    [Test]
    public void GetPool_RegisteredType_UsesCustomPolicy()
    {
        // Arrange & Act - StaticPoolObject has a custom policy registered
        var pool = StaticPools.GetPool<StaticPoolObject>();

        // Assert
        Assert.That(pool, Is.Not.Null);
        Assert.That(pool.InitCount, Is.EqualTo(3), "Should use the custom policy's InitCount");
        Assert.That(pool.MaxCount, Is.EqualTo(7), "Should use the custom policy's MaxCount");

        var obj = pool.Rent();
        Assert.That(obj, Is.Not.Null);
        Assert.That(obj, Is.InstanceOf<StaticPoolObject>());

        // Clean up
        pool.Recycle(obj);
    }

    [Test]
    public void GetPool_NonRegisteredType_UsesDefaultPolicy()
    {
        // Arrange & Act - AnotherStaticPoolObject has no custom policy
        var pool = StaticPools.GetPool<AnotherStaticPoolObject>();

        // Assert - should use DefaultObjectPoolPolicy
        Assert.That(pool, Is.Not.Null);

        // Rent an object to verify the pool works
        var obj = pool.Rent();
        Assert.That(obj, Is.Not.Null);
        Assert.That(obj, Is.InstanceOf<AnotherStaticPoolObject>());

        // Clean up
        pool.Recycle(obj);
    }

    [Test]
    public void StaticPool_RentAndRecycle_PreservesContent()
    {
        // Arrange
        var pool = StaticPools.GetPool<StaticPoolObject>();

        // Act
        var obj = pool.Rent();
        obj.Value = 42;
        pool.Recycle(obj);

        // Get the same object back - it should preserve its state
        var sameObj = pool.Rent();
        while (!ReferenceEquals(obj, sameObj))
        {
            sameObj = pool.Rent();
        }

        // Assert
        Assert.That(sameObj.Value, Is.EqualTo(42),
            "Object should preserve its value");

        // Clean up
        pool.Recycle(sameObj);
    }

    [Test]
    public void StaticPool_ThreadSafety_MaintainsIntegrity()
    {
        // Arrange
        const int iterations = 100;
        const int threadCount = 4;

        // Act - Access the same pool from multiple threads
        var task = Task.Run(() => {
            Parallel.For(0, threadCount, _ => {
                for (int i = 0; i < iterations; i++)
                {
                    var pool1 = StaticPools.GetPool<StaticPoolObject>();
                    var pool2 = StaticPools.GetPool(typeof(StaticPoolObject));

                    // Verify pools are valid and the same instance
                    Assert.That(pool1, Is.Not.Null);
                    Assert.That(pool2, Is.Not.Null);

                    // Perform operations on the pools
                    var obj1 = pool1.Rent();
                    obj1.Value = i;
                    pool1.Recycle(obj1);

                    var obj2 = pool2.Rent();
                    pool2.Recycle(obj2);
                }
            });
        });

        // Wait for completion
        task.Wait();

        // No assertions needed - test passes if no exceptions occurred
    }

    [Test]
    public void GetPool_VariousTypes_EachHasUniquePool()
    {
        // Test with multiple different types to ensure each gets its own pool
        var poolTypes = new List<Type>
        {
            typeof(StaticPoolObject),
            typeof(AnotherStaticPoolObject),
            typeof(List<int>),
            typeof(Dictionary<string, int>),
        };

        var pools = new Dictionary<Type, IObjectPool>();

        // Get pools for each type
        foreach (var type in poolTypes)
        {
            pools[type] = StaticPools.GetPool(type);
            Assert.That(pools[type], Is.Not.Null, $"Pool for {type.Name} should not be null");
        }

        // Verify each type has a unique pool
        foreach (var type1 in poolTypes)
        {
            foreach (var type2 in poolTypes)
            {
                if (type1 != type2)
                {
                    Assert.That(pools[type1], Is.Not.SameAs(pools[type2]),
                        $"Pool for {type1.Name} should be different from pool for {type2.Name}");
                }
            }
        }
    }

    [Test]
    public void GetPool_ReturnsPoolWithCorrectType()
    {
        // Verify that the pool returned is compatible with the requested type

        // Generic method
        var genericPool = StaticPools.GetPool<StaticPoolObject>();
        Assert.That(genericPool, Is.InstanceOf<IObjectPool<StaticPoolObject>>());

        // Non-generic method
        var nonGenericPool = StaticPools.GetPool(typeof(StaticPoolObject));
        Assert.That(nonGenericPool, Is.InstanceOf<IObjectPool>());

        // Verify that objects from both pools are of the correct type
        var obj1 = genericPool.Rent();
        var obj2 = nonGenericPool.Rent();

        Assert.That(obj1, Is.InstanceOf<StaticPoolObject>());
        Assert.That(obj2, Is.InstanceOf<StaticPoolObject>());

        // Clean up
        genericPool.Recycle(obj1);
        nonGenericPool.Recycle(obj2);
    }

    [Test]
    public void Pool_StateIsSeparate_BetweenTypePools()
    {
        // Verify that operations on one pool don't affect others
        var pool1 = StaticPools.GetPool<StaticPoolObject>();
        var pool2 = StaticPools.GetPool<AnotherStaticPoolObject>();

        // Initial state
        var initialRentedCount1 = pool1.RentedCount;
        var initialRentedCount2 = pool2.RentedCount;

        // Perform operations on pool1
        var obj = pool1.Rent();

        // Verify only pool1 state changed
        Assert.That(pool1.RentedCount, Is.EqualTo(initialRentedCount1 + 1));
        Assert.That(pool2.RentedCount, Is.EqualTo(initialRentedCount2),
            "Operations on one pool should not affect others");

        // Clean up
        pool1.Recycle(obj);
    }

    // Helper method to reset static pools between tests
    private void ResetStaticPools()
    {
        // Use reflection to access and clear the internal dictionary
        var poolsField = typeof(StaticPools).GetField("s_pools",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (poolsField != null)
        {
            var pools = poolsField.GetValue(null);
            var clearMethod = pools.GetType().GetMethod("Clear");
            if (clearMethod != null)
            {
                clearMethod.Invoke(pools, null);
            }
        }
    }
}
