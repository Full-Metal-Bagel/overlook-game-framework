#nullable enable

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Overlook.Pool;

public sealed class TypePoolsCache : IDisposable
{
    private readonly ConcurrentDictionary<Type, IObjectPool> _pools = new();
    private static readonly ThreadLocal<object[]> s_objectPoolParameters = new(() => new object[1]);

    public IObjectPool GetOrCreate(Type type, IObjectPoolPolicy policy)
    {
        var pool = _pools.GetOrAdd(type, static (type, p) => CreatePool(type: type, policy: p), policy);
        Debug.Assert(ReferenceEquals(pool.Policy, policy), $"Pool for type {type} was created with policy {pool.Policy} but expected {policy}");
        return pool;
    }

    public IObjectPool<T> GetOrCreate<T>(IObjectPoolPolicy policy) where T : class
    {
        var pool = (IObjectPool<T>)_pools.GetOrAdd
        (
            typeof(T),
            static (_, p) => new DefaultObjectPool<T>(p),
            policy
        );
        Debug.Assert(ReferenceEquals(pool.Policy, policy), $"Pool for type {typeof(T)} was created with policy {pool.Policy} but expected {policy}");
        return pool;
    }

    public void Clear(Type type)
    {
        if (_pools.TryGetValue(type, out var pool)) pool.Dispose();
    }

    public void Dispose()
    {
        foreach (var pool in _pools.Values)
        {
            if (pool is IDisposable disposable) disposable.Dispose();
        }
        _pools.Clear();
    }

    private static IObjectPool CreatePool(Type type, IObjectPoolPolicy policy)
    {
        // TODO: optimize reflection?
        var createMethod = typeof(DefaultObjectPool<>).MakeGenericType(type).GetConstructors().Single();
        s_objectPoolParameters.Value[0] = policy;
        return (IObjectPool)createMethod.Invoke(null, s_objectPoolParameters.Value);
    }
}
