using System;
using Game;
using NUnit.Framework;

[TestFixture]
public class TestPools
{
    private class TestObject { }
    private class TestObject2 { }
    private class TestObjectWithoutDefaultConstructor
    {
        public TestObjectWithoutDefaultConstructor(int _) { }
    }

    [Test]
    public void Get_ReturnsSamePool_ForSameType()
    {
        var pools = new Game.TypePoolsCache();
        var pool1 = pools.GetOrCreate<TestObject>();
        var pool2 = pools.GetOrCreate<TestObject>();
        Assert.That(pool2, Is.SameAs(pool1));
    }

    [Test]
    public void Get_ReturnsCorrectPoolType()
    {
        var pools = new Game.TypePoolsCache();
        var pool = pools.GetOrCreate<TestObject>();
        Assert.IsInstanceOf<IObjectPool<TestObject>>(pool);
    }

    [Test]
    public void Get_WithType_ReturnsCorrectPool()
    {
        var pools = new Game.TypePoolsCache();
        var pool = pools.GetOrCreate(typeof(TestObject));
        Assert.IsInstanceOf<IObjectPool>(pool);
    }

    [Test]
    public void Get_WithCustomCreateFunc_UsesProvidedFunc()
    {
        var pools = new Game.TypePoolsCache();
        var customObject = new TestObject();
        var pool = pools.GetOrCreate(() => customObject);
        var rentedObject = pool.Rent();
        Assert.That(rentedObject, Is.SameAs(customObject));
    }

    [Test]
    public void Dispose_DisposesAllPools()
    {
        var pools = new Game.TypePoolsCache();
        var pool1 = pools.GetOrCreate<TestObject>();
        var pool2 = pools.GetOrCreate<TestObject2>();

        pools.Dispose();

        Assert.Catch<Exception>(() => pool1.Rent());
        Assert.Catch<Exception>(() => pool2.Rent());
    }

    [Test]
    public void Get_WithCustomParameters_CreatesPoolCorrectly()
    {
        var pools = new Game.TypePoolsCache();
        int initCount = 0;
        int maxCount = 10;
        Func<int, int> expandFunc = count => count + 1;

        var pool = pools.GetOrCreate<TestObject>(
            onRentAction: _ => { },
            onRecycleAction: _ => { },
            initCount: initCount,
            maxCount: maxCount,
            expandFunc: expandFunc
        );

        Assert.IsNotNull(pool);
        Assert.That(pool.MaxCount, Is.EqualTo(maxCount));
        Assert.That(pool.ExpandFunc, Is.EqualTo(expandFunc));
    }

    [Test]
    public void Get_WithTypeWithoutParameterlessConstructor_ThrowsArgumentException()
    {
        var pools = new TypePoolsCache();

        var exception = Assert.Throws<ArgumentException>(() =>
        {
            pools.GetOrCreate(typeof(TestObjectWithoutDefaultConstructor));
        });

        Assert.That(exception.Message, Does.Contain("must have a parameterless constructor"));
        Assert.That(exception.ParamName, Is.EqualTo("type"));
        Assert.That(exception.InnerException, Is.InstanceOf<ArgumentException>());
    }
}
