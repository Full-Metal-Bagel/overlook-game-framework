using System.Collections.Generic;
using NUnit.Framework;
using Game;

public class PooledListTests
{
    [Test]
    public void Constructor_CreatesList()
    {
        using var pooledList = new PooledList<int>(1);
        Assert.IsNotNull(pooledList.GetValue());
    }

    [Test]
    public void Add_AddsItemToList()
    {
        using var pooledList = new PooledList<int>(1);
        pooledList.Add(1);
        Assert.That(pooledList.Count, Is.EqualTo(1));
        Assert.That(pooledList[0], Is.EqualTo(1));
    }

    [Test]
    public void Remove_RemovesItemFromList()
    {
        using var pooledList = new PooledList<int>(1);
        pooledList.Add(1);
        pooledList.Remove(1);
        Assert.That(pooledList.Count, Is.EqualTo(0));
    }

    [Test]
    public void Clear_ClearsList()
    {
        using var pooledList = new PooledList<int>(1);
        pooledList.Add(1);
        pooledList.Clear();
        Assert.That(pooledList.Count, Is.EqualTo(0));
    }

    [Test]
    public void Dispose_ReleasesListBackToPool()
    {
        var pooledList = new PooledList<int>(1);
        pooledList.Dispose();
        try
        {
            pooledList.GetValue();
        }
        catch (PooledCollectionException)
        {
            return;
        }
        Assert.Fail("PooledCollectionException not thrown");
    }

    // Additional tests for CopyTo, Contains, IndexOf, Insert, RemoveAt, etc.
    // ...

    [Test]
    public void ReusingDisposedCollection_ThrowsException()
    {
        var pooledList = new PooledList<int>(1);
        pooledList.Dispose();
        try
        {
            pooledList.Add(1);
        }
        catch (PooledCollectionException)
        {
            return;
        }
        Assert.Fail("PooledCollectionException not thrown");
    }

    [Test]
    public void AccessingCollectionAfterDispose_ThrowsException()
    {
        var pooledList = new PooledList<int>(1);
        pooledList.Dispose();
        try
        {
            var _ = pooledList[0];
        }
        catch (PooledCollectionException)
        {
            return;
        }
        Assert.Fail("PooledCollectionException not thrown");
    }

    // If applicable, tests for DISABLE_POOLED_COLLECTIONS_CHECKS logic.
    // ...
    [Test]
    public void UsingPattern_ReleasesListBackToPool()
    {
        List<int> listReference;

        // Using the PooledList inside a using block
        using (var pooledList = new PooledList<int>(1))
        {
            pooledList.Add(1);
            listReference = pooledList.GetValue();
        }

        Assert.That(listReference.Count, Is.Zero);
    }

    [Test]
    public void UsingPattern_AllowsForMultipleUsages()
    {
        using (var pooledList1 = new PooledList<int>(1))
        {
            pooledList1.Add(1);
            Assert.That(pooledList1.Count, Is.EqualTo(1));
        }

        // Using another PooledList to ensure that lists can be reused after being disposed
        using (var pooledList2 = new PooledList<int>(1))
        {
            pooledList2.Add(2);
            Assert.That(pooledList2.Count, Is.EqualTo(1));
            Assert.That(pooledList2[0], Is.EqualTo(2));
        }
    }

}
