using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Overlook.Pool;
using Overlook.Pool.Tests;

[assembly: OverridePoolPolicy<TestObjectB, CustomTestPoolPolicy>]

namespace Overlook.Pool.Tests;

public class TestObjectA { }
public class TestObjectB { }

public struct CustomTestPoolPolicy : IObjectPoolPolicy
{
    public int InitCount => 7;
    public int MaxCount => 25;
    public int Expand(int size) => size + 5;
    public object Create() => new TestObjectB();
}

[TestFixture]
public class DefaultObjectPoolProviderTests
{
    [Test]
    public void Get_GenericMethod_ReturnsCorrectProviderType()
    {
        // Act
        var provider = ObjectPoolProvider.Get<TestObjectA>();

        // Assert
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider, Is.InstanceOf<DefaultObjectPoolProvider<TestObjectA>>());
        Assert.That(provider.ObjectType, Is.EqualTo(typeof(TestObjectA)));
    }

    [Test]
    public void Get_TypeParameter_ReturnsCorrectProviderType()
    {
        // Act
        var provider = ObjectPoolProvider.Get(typeof(TestObjectA));

        // Assert
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider, Is.InstanceOf<DefaultObjectPoolProvider<TestObjectA>>());
        Assert.That(provider.ObjectType, Is.EqualTo(typeof(TestObjectA)));
    }

    [Test]
    public void Get_SameType_ReturnsCachedProvider()
    {
        // Act
        var provider1 = ObjectPoolProvider.Get<TestObjectA>();
        var provider2 = ObjectPoolProvider.Get<TestObjectA>();
        var provider3 = ObjectPoolProvider.Get(typeof(TestObjectA));

        // Assert
        Assert.That(provider2, Is.SameAs(provider1), "Same instance should be returned for identical generic calls");
        Assert.That(provider3, Is.SameAs(provider1), "Same instance should be returned regardless of how the provider is requested");
    }

    [Test]
    public void CreatePool_ReturnsCorrectPoolType()
    {
        // Arrange
        var provider = ObjectPoolProvider.Get<TestObjectA>();

        // Act
        var pool = provider.CreatePool();

        // Assert
        Assert.That(pool, Is.Not.Null);
        Assert.That(pool, Is.InstanceOf<ObjectPool<TestObjectA, DefaultObjectPoolPolicy<TestObjectA>>>());
    }

    [Test]
    public void RegisterDefaultObjectPoolAttribute_IsRespectedForRegisteredTypes()
    {
        // Arrange & Act - TestObjectB has a custom policy registered via attribute
        var provider = ObjectPoolProvider.Get<TestObjectB>();
        var pool = provider.CreatePool();

        // Assert
        Assert.That(provider, Is.Not.Null);
        Assert.That(pool, Is.Not.Null);
        Assert.That(pool, Is.InstanceOf<ObjectPool<TestObjectB, CustomTestPoolPolicy>>());
    }

    [Test]
    public void ProviderCache_HasEntriesForAttributeRegisteredTypes()
    {
        // Access the private cache to verify it contains our attribute-registered type
        var providerCacheField = typeof(ObjectPoolProvider)
            .GetField("s_providerCache", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(providerCacheField, Is.Not.Null, "Could not find s_providerCache field");

        var providerCache = providerCacheField.GetValue(null);
        var containsMethodInfo = providerCache.GetType().GetMethod("ContainsKey");

        Assert.That(containsMethodInfo, Is.Not.Null, "Could not find ContainsKey method");

        var hasTestObjectBProvider = (bool)containsMethodInfo.Invoke(providerCache, new object[] { typeof(TestObjectB) });

        Assert.That(hasTestObjectBProvider, Is.True,
            "Provider cache should contain an entry for TestObjectB from attribute registration");
    }

    [Test]
    public void Get_WithCustomTypeHandling_WorksWithThreadLocalTypeParameters()
    {
        // Act - This specifically tests the thread-local type parameter optimization
        var results = new List<IObjectPoolProvider>();

        // Run in parallel to ensure thread safety
        System.Threading.Tasks.Parallel.For(0, 10, _ => {
            results.Add(ObjectPoolProvider.Get(typeof(TestObjectA)));
        });

        // Assert
        Assert.That(results, Has.Count.EqualTo(10));
        Assert.That(results, Is.All.Not.Null);
        Assert.That(results, Is.All.InstanceOf<DefaultObjectPoolProvider<TestObjectA>>());

        // All instances should be the same (cached)
        var first = results[0];
        Assert.That(results, Is.All.SameAs(first));
    }

    [Test]
    public void Provider_ObjectType_ReturnsCorrectType()
    {
        // Arrange
        var providerA = DefaultObjectPoolProvider.Get<TestObjectA>();
        var providerB = DefaultObjectPoolProvider.Get<TestObjectB>();

        // Assert
        Assert.That(providerA.ObjectType, Is.EqualTo(typeof(TestObjectA)));
        Assert.That(providerB.ObjectType, Is.EqualTo(typeof(TestObjectB)));
    }
}
