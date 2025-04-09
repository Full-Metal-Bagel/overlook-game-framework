using System;

namespace Overlook.Pool;

public static class StaticPools
{
    private static readonly TypeObjectPoolCache s_cache = new();

    public static IObjectPool<T> GetPool<T>() where T : class, new()
    {
        return Cache<T>.Instance;
    }

    public static IObjectPool GetPool(Type type)
    {
        return s_cache.GetPool(type);
    }

    public static void Clear()
    {
        s_cache.Dispose();
    }

    private static class Cache<T> where T : class, new()
    {
        public static IObjectPool<T> Instance { get; } = s_cache.GetPool<T>();
    }
}
