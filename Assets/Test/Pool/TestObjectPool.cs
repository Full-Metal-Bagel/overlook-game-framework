using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using System;
using Overlook.Pool;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
using UnityEngine.TestTools;
#endif

[TestFixture]
public class ObjectPoolTests
{
    private sealed class TestObject : IDisposable, IObjectPoolCallback
    {
        public bool IsDisposed { get; private set; }
        public bool OnRentCalled { get; private set; }
        public bool OnRecycleCalled { get; private set; }
        public void Dispose() => IsDisposed = true;
        public void OnRent() => OnRentCalled = true;
        public void OnRecycle() => OnRecycleCalled = true;
    }

    [Test]
    public void Rent_ReturnsNewObject_WhenPoolIsEmpty()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject());
        var obj = pool.Rent();
        Assert.IsNotNull(obj);
    }

    [Test]
    public void Recycle_ReturnsObjectToPool()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject());
        var obj = pool.Rent();
        pool.Recycle(obj);
        var obj2 = pool.Rent();
        Assert.That(obj, Is.SameAs(obj2));
    }

    [Test]
    public void Rent_CallsOnRentAction()
    {
        bool onRentCalled = false;
        var pool = new ObjectPool<TestObject>(() => new TestObject(), onRentAction: _ => onRentCalled = true);
        pool.Rent();
        Assert.That(onRentCalled, Is.True);
    }

    [Test]
    public void Recycle_CallsOnRecycleAction()
    {
        bool onRecycleCalled = false;
        var pool = new ObjectPool<TestObject>(() => new TestObject(), onRecycleAction: _ => onRecycleCalled = true);
        var obj = pool.Rent();
        pool.Recycle(obj);
        Assert.That(onRecycleCalled, Is.True);
    }

    [Test]
    public void MaxCount_LimitsPoolSize()
    {
        var createCount = 0;
        var pool = new ObjectPool<TestObject>(() => { createCount++; return new TestObject(); }, maxCount: 2);
        var obj1 = pool.Rent();
        var obj2 = pool.Rent();
        var obj3 = pool.Rent();
        pool.Recycle(obj1);
        pool.Recycle(obj2);
        pool.Recycle(obj3);
        Assert.That(pool.MaxCount, Is.EqualTo(2));
        Assert.That(createCount, Is.EqualTo(3));
    }

    [Test]
    public void ExpandFunc_IncreasesPoolSize()
    {
        var createCount = 0;
        var pool = new ObjectPool<TestObject>(() => { createCount++; return new TestObject(); }, expandFunc: count => count * 2);
        var objects = new List<TestObject>();
        for (int i = 0; i < 5; i++)
        {
            objects.Add(pool.Rent());
        }
        Assert.That(createCount, Is.EqualTo(8));
    }

    [Test]
    public void Dispose_ClearsPool()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject());
        var obj = pool.Rent();
        pool.Recycle(obj);
        pool.Dispose();
#if UNITY_5_3_OR_NEWER
        LogAssert.Expect(LogType.Assert, "pool had been disposed already");
#else
        Assert.Catch<Exception>(() => pool.Rent());
#endif
    }

    [Test]
    public void Dispose_DisposesObjects()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject());
        var obj = pool.Rent();
        pool.Recycle(obj);
        pool.Dispose();
        Assert.IsTrue(obj.IsDisposed);
    }

    [Test]
    public void Recycle_DisposesObject_WhenPoolIsFull()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject(), maxCount: 1);
        var obj1 = pool.Rent();
        var obj2 = pool.Rent();
        pool.Recycle(obj1);
        pool.Recycle(obj2);
        Assert.IsTrue(obj2.IsDisposed);
    }

    [Test]
    public void Rent_CallsIObjectPoolCallbackOnRent()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject());
        var obj = pool.Rent();
        Assert.IsTrue(obj.OnRentCalled);
    }

    [Test]
    public void Recycle_CallsIObjectPoolCallbackOnRecycle()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject());
        var obj = pool.Rent();
        pool.Recycle(obj);
        Assert.IsTrue(obj.OnRecycleCalled);
    }

    [Test]
    public void InitCount_CreatesInitialObjects()
    {
        var createCount = 0;
        var pool = new ObjectPool<TestObject>(() => { createCount++; return new TestObject(); }, initCount: 5);
        Assert.That(createCount, Is.EqualTo(5));
    }

    [Test]
    public void ConcurrentAccess_WorksCorrectly()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject(), maxCount: 100);
        var tasks = new List<Task>();

        for (int i = 0; i < 1000; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var obj = pool.Rent();
                pool.Recycle(obj);
            }));
        }

        Task.WaitAll(tasks.ToArray());
        Assert.Pass();
    }
}
