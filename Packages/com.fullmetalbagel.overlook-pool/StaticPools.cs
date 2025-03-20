using System;
using System.Collections.Concurrent;

namespace Overlook.Pool;

public static class StaticPools
{
    private static readonly ConcurrentDictionary<Type, IObjectPool> s_pools = new();

    public static IObjectPool<T> GetPool<T>() where T : class, new()
    {
        return Cache<T>.Instance;
    }

    public static IObjectPool GetPool(Type type)
    {
        return s_pools.GetOrAdd(type, static type => DefaultObjectPoolProvider.Get(type).CreatePool());
    }

    private static IObjectPool<T> GetOrAdd<T>() where T : class, new()
    {
        return (IObjectPool<T>)s_pools.GetOrAdd(typeof(T), static _ => DefaultObjectPoolProvider.Get<T>().CreatePool());
    }

    private static class Cache<T> where T : class, new()
    {
        public static IObjectPool<T> Instance { get; } = GetOrAdd<T>();
    }
}
