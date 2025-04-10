using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Overlook.Pool.Tests;

// Test objects for pool usage
public class SimplePoolObject
{
    public int Value { get; set; }
}

public class DisposablePoolObject : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

public class CallbackPoolObject : IObjectPoolCallback
{
    public bool WasRented { get; private set; }
    public bool WasRecycled { get; private set; }
    public int RentCount { get; private set; }
    public int RecycleCount { get; private set; }

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
}

// Custom policies for testing
public struct EmptyInitPoolPolicy : IObjectPoolPolicy
{
    public int InitCount => 0;
    public int MaxCount => 10;
    public int Expand(int size) => size + 1;
    public object Create() => new SimplePoolObject();
}

public struct PreloadedPoolPolicy : IObjectPoolPolicy
{
    public int InitCount => 5;
    public int MaxCount => 10;
    public int Expand(int size) => size + 2;
    public object Create() => new SimplePoolObject();
}

public struct LimitedPoolPolicy : IObjectPoolPolicy
{
    public int InitCount => 2;
    public int MaxCount => 5;
    public int Expand(int size) => size + 1;
    public object Create() => new SimplePoolObject();
}

public struct DisposableObjectPolicy : IObjectPoolPolicy
{
    public int InitCount => 2;
    public int MaxCount => 5;
    public int Expand(int size) => size + 1;
    public object Create() => new DisposablePoolObject();
}

public struct CallbackObjectPolicy : IObjectPoolPolicy
{
    public int InitCount => 1;
    public int MaxCount => 1;
    public int Expand(int size) => size;
    public object Create() => new CallbackPoolObject();
}

[TestFixture]
public class ObjectPoolTests
{
    [Test]
    public void Initialize_WithPreloadedPolicy_PreallocatesObjects()
    {
        // Arrange & Act
        var pool = new ObjectPool<SimplePoolObject, PreloadedPoolPolicy>();

        // Assert
        Assert.That(pool.InitCount, Is.EqualTo(5));
        Assert.That(pool.MaxCount, Is.EqualTo(10));
        Assert.That(pool.PooledCount, Is.EqualTo(5), "Pool should be pre-loaded with objects");
        Assert.That(pool.RentedCount, Is.EqualTo(0), "No objects should be rented yet");
    }

    [Test]
    public void Initialize_WithEmptyPolicy_HasNoObjects()
    {
        // Arrange & Act
        var pool = new ObjectPool<SimplePoolObject, EmptyInitPoolPolicy>();

        // Assert
        Assert.That(pool.InitCount, Is.EqualTo(0));
        Assert.That(pool.MaxCount, Is.EqualTo(10));
        Assert.That(pool.PooledCount, Is.EqualTo(0), "Pool should be empty");
        Assert.That(pool.RentedCount, Is.EqualTo(0), "No objects should be rented");
    }

    [Test]
    public void Rent_FromPreloadedPool_ReturnsPreallocatedObject()
    {
        // Arrange
        var pool = new ObjectPool<SimplePoolObject, PreloadedPoolPolicy>();
        var initialPooledCount = pool.PooledCount;

        // Act
        var obj = pool.Rent();

        // Assert
        Assert.That(obj, Is.Not.Null);
        Assert.That(pool.PooledCount, Is.EqualTo(initialPooledCount - 1), "Pooled count should decrease by 1");
        Assert.That(pool.RentedCount, Is.EqualTo(1), "Rented count should increase to 1");
    }

    [Test]
    public void Rent_FromEmptyPool_CreatesNewObject()
    {
        // Arrange
        var pool = new ObjectPool<SimplePoolObject, EmptyInitPoolPolicy>();

        // Act
        var obj = pool.Rent();

        // Assert
        Assert.That(obj, Is.Not.Null);
        Assert.That(pool.PooledCount, Is.EqualTo(0), "Pooled count should remain 0");
        Assert.That(pool.RentedCount, Is.EqualTo(1), "Rented count should increase to 1");
    }

    [Test]
    public void Rent_MultipleTimes_ExpandsPool()
    {
        // Arrange
        var pool = new ObjectPool<SimplePoolObject, EmptyInitPoolPolicy>();

        // Act - Rent more objects than initial capacity
        var objects = new List<SimplePoolObject>();
        for (int i = 0; i < 3; i++)
        {
            objects.Add(pool.Rent());
        }

        // Assert
        Assert.That(objects, Has.Count.EqualTo(3));
        Assert.That(objects, Is.All.Not.Null);
        Assert.That(objects, Is.Unique);
        Assert.That(pool.RentedCount, Is.EqualTo(3));
        Assert.That(pool.PooledCount, Is.EqualTo(0));
    }

    [Test]
    public void Recycle_ReturnedObject_IncreasesPooledCount()
    {
        // Arrange
        var pool = new ObjectPool<SimplePoolObject, EmptyInitPoolPolicy>();
        var obj = pool.Rent();
        int initialPooledCount = pool.PooledCount;

        // Act
        pool.Recycle(obj);

        // Assert
        Assert.That(pool.PooledCount, Is.EqualTo(initialPooledCount + 1), "Pooled count should increase");
        Assert.That(pool.RentedCount, Is.EqualTo(0), "Rented count should decrease");
    }

    [Test]
    public void RentAndRecycle_MultipleObjects_MaintainsCorrectCounts()
    {
        // Arrange
        var pool = new ObjectPool<SimplePoolObject, LimitedPoolPolicy>();

        // Act & Assert - Rent all initial objects
        var obj1 = pool.Rent();
        var obj2 = pool.Rent();

        Assert.That(pool.PooledCount, Is.EqualTo(0), "All initial objects should be rented");
        Assert.That(pool.RentedCount, Is.EqualTo(2), "Two objects should be rented");

        // Rent beyond initial capacity, which should expand pool
        var obj3 = pool.Rent();

        Assert.That(pool.RentedCount, Is.EqualTo(3), "Three objects should be rented");
        Assert.That(pool.PooledCount, Is.EqualTo(0));

        // Recycle objects
        pool.Recycle(obj1);
        pool.Recycle(obj2);

        Assert.That(pool.RentedCount, Is.EqualTo(1), "One object should remain rented");
        Assert.That(pool.PooledCount, Is.EqualTo(2), "Two objects should be back in pool");

        // Rent again - should get recycled objects
        var obj4 = pool.Rent();
        var obj5 = pool.Rent();

        Assert.That(pool.RentedCount, Is.EqualTo(3), "Three objects should be rented");
        Assert.That(pool.PooledCount, Is.EqualTo(0), "No objects should be in pool");

        // Verify one of the objects is reused
        Assert.That(new[] { obj4, obj5 }, Is.EquivalentTo(new[] { obj1, obj2 }));
    }

    [Test]
    public void Recycle_BeyondMaxCapacity_DisposesObject()
    {
        // Arrange
        var pool = new ObjectPool<DisposablePoolObject, DisposableObjectPolicy>();

        // Fill the pool to max capacity
        var objects = new List<DisposablePoolObject>();
        for (int i = 0; i < pool.MaxCount + 3; i++)
        {
            objects.Add(pool.Rent());
        }

        // Now recycle all objects - some should be disposed when the pool is full
        foreach (var obj in objects)
        {
            pool.Recycle(obj);
        }

        // Assert
        Assert.That(pool.PooledCount, Is.EqualTo(pool.MaxCount),
            "Pool should contain exactly MaxCount objects");

        // At least some objects should be disposed
        Assert.That(objects, Has.Some.Matches<DisposablePoolObject>(o => o.IsDisposed),
            "Objects beyond max capacity should be disposed");

        // The objects in the pool should not be disposed
        var remainingObjects = new List<DisposablePoolObject>();
        for (int i = 0; i < pool.MaxCount; i++)
        {
            remainingObjects.Add(pool.Rent());
        }

        Assert.That(remainingObjects, Is.All.Matches<DisposablePoolObject>(o => !o.IsDisposed),
            "Objects in the pool should not be disposed");
    }

    [Test]
    public void Dispose_Pool_DisposesAllObjects()
    {
        // Arrange
        var pool = new ObjectPool<DisposablePoolObject, DisposableObjectPolicy>();

        // Rent and recycle to ensure pool has some objects
        var obj1 = pool.Rent();
        var obj2 = pool.Rent();
        pool.Recycle(obj1);

        // Act
        pool.Dispose();

        // Assert
        Assert.That(obj1.IsDisposed, Is.True, "Recycled object should be disposed");
        Assert.That(obj2.IsDisposed, Is.False, "Rented object should not be disposed");
        Assert.That(pool.PooledCount, Is.EqualTo(0), "Pool should be empty after disposal");
    }

    [Test]
    public void ObjectPoolCallback_IsInvoked_OnRentAndRecycle()
    {
        // Arrange
        var pool = new ObjectPool<CallbackPoolObject, CallbackObjectPolicy>();

        // Act
        var obj = pool.Rent();

        // Assert
        Assert.That(obj.WasRented, Is.True, "OnRent should be called");
        Assert.That(obj.RentCount, Is.EqualTo(1), "OnRent should be called exactly once");
        Assert.That(obj.WasRecycled, Is.False, "OnRecycle should not be called yet");

        // Act
        pool.Recycle(obj);

        // Assert
        Assert.That(obj.WasRecycled, Is.True, "OnRecycle should be called");
        Assert.That(obj.RecycleCount, Is.EqualTo(1), "OnRecycle should be called exactly once");

        // Rent the same object again
        var obj2 = pool.Rent();

        // It should be the same object with updated callback counts
        Assert.That(obj2, Is.SameAs(obj), "The same object should be returned from the pool");
        Assert.That(obj2.RentCount, Is.EqualTo(2), "OnRent should be called a second time");
    }

    [Test]
    public void NonGenericInterface_WorksCorrectly()
    {
        // Arrange
        var pool = new ObjectPool<SimplePoolObject, PreloadedPoolPolicy>();
        IObjectPool nonGenericPool = pool;

        // Act
        var obj = nonGenericPool.Rent();

        // Assert
        Assert.That(obj, Is.Not.Null);
        Assert.That(obj, Is.InstanceOf<SimplePoolObject>());
        Assert.That(nonGenericPool.RentedCount, Is.EqualTo(1));

        // Act
        nonGenericPool.Recycle(obj);

        // Assert
        Assert.That(nonGenericPool.RentedCount, Is.EqualTo(0));
        Assert.That(nonGenericPool.PooledCount, Is.EqualTo(pool.InitCount));
    }

    [Test]
    public void ThreadSafety_ConcurrentRentAndRecycle()
    {
        // Arrange
        var pool = new ObjectPool<SimplePoolObject, LimitedPoolPolicy>();
        const int iterations = 1000;
        const int threadCount = 8;

        // Act
        var task = Task.Run(() => {
            Parallel.For(0, threadCount, _ => {
                var threadLocalObjects = new List<SimplePoolObject>();
                for (int i = 0; i < iterations; i++)
                {
                    // Rent an object
                    var obj = pool.Rent();
                    // Do something with it
                    obj.Value = i;
                    threadLocalObjects.Add(obj);

                    // Occasionally recycle some objects
                    if (i % 10 == 0 && threadLocalObjects.Count > 0)
                    {
                        var objToRecycle = threadLocalObjects[0];
                        threadLocalObjects.RemoveAt(0);
                        pool.Recycle(objToRecycle);
                    }
                }

                // Recycle all remaining objects
                foreach (var obj in threadLocalObjects)
                {
                    pool.Recycle(obj);
                }
            });
        });

        // Wait for all threads to complete
        task.Wait();

        // Assert
        Assert.That(pool.RentedCount, Is.EqualTo(0), "All objects should be returned to the pool");
        Assert.That(pool.PooledCount, Is.AtMost(pool.MaxCount), "Pool should respect maximum capacity");
    }

    [Test]
    public void Expansion_RespectsPolicyRules()
    {
        // Arrange - use a policy that expands by 2
        var pool = new ObjectPool<SimplePoolObject, PreloadedPoolPolicy>();
        int initialPooledCount = pool.PooledCount;

        // Act - rent all initial objects plus one more
        List<SimplePoolObject> objects = new List<SimplePoolObject>();
        for (int i = 0; i < initialPooledCount + 1; i++)
        {
            objects.Add(pool.Rent());
        }

        // Assert - pool should have expanded by policy rules (+2)
        // The expansion creates N+1 objects, where 1 is immediately returned
        // and N are added to the pool
        Assert.That(pool.PooledCount, Is.EqualTo(1),
            "Pool should expand by 2 according to policy");

        // Return one object
        pool.Recycle(objects[0]);

        // Assert
        Assert.That(pool.PooledCount, Is.EqualTo(2),
            "Pool should have 2 expanded objects plus 1 recycled");
    }
}
