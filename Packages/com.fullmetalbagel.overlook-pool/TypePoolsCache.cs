#nullable enable

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Overlook.Pool;

[SuppressMessage("Naming", "CA1724:The type name Pools conflicts in whole or in part with the namespace name 'FluffyUnderware.Curvy.Pools'")]
public sealed class TypePoolsCache : IDisposable
{
    private readonly ConcurrentDictionary<Type, IObjectPool> _pools = new();

    public IObjectPool GetOrCreate(Type type, IObjectPoolPolicy policy)
    {
        return _pools.GetOrAdd(type, static (type, p) => CreatePool(type: type, policy: p), policy);
    }

    public IObjectPool<T> GetOrCreate<T>(IObjectPoolPolicy policy) where T : class
    {
        return (IObjectPool<T>)_pools.GetOrAdd
        (
            typeof(T),
            static (_, p) => new ObjectPool<T>(p),
            policy
        );
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
        var createMethod = typeof(ObjectPool<>).MakeGenericType(type).GetConstructors().Single();
        return (IObjectPool)createMethod.Invoke(null, new object?[] { policy });
    }
}
