using System;

namespace Overlook.Pool;

public static class StaticPools
{
    private static readonly TypeObjectPoolCache s_cache = new();

    public static IObjectPool<T> GetPool<T>() where T : class, new()
    {
        return s_cache.GetPool<T>();
    }

    public static IObjectPool GetPool(Type type)
    {
        return s_cache.GetPool(type);
    }

    public static void Clear()
    {
        s_cache.Dispose();
    }
}
