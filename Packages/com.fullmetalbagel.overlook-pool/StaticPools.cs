using System;
using System.Diagnostics.CodeAnalysis;

namespace Overlook.Pool;

public static class StaticPools
{
    private static readonly TypePoolsCache s_pools = new();

    internal static IObjectPool<T> Get<T>() where T : class, new()
    {
        var policy = StaticObjectPoolPolicy.Get(typeof(T));
        return s_pools.GetOrCreate<T>(policy);
    }

    private static IObjectPool Get(Type type)
    {
        var policy = StaticObjectPoolPolicy.Get(type);
        return s_pools.GetOrCreate(type, policy);
    }

    public static object Rent(Type type) => Get(type).Rent();
    public static void Recycle(Type type, object instance) => Get(type).Recycle(instance);
    public static int RentedCount(Type type) => Get(type).RentedCount;
    public static int PooledCount(Type type) => Get(type).PooledCount;
    public static void Clear(Type type) => s_pools.Clear(type);

    public static void Clear() => s_pools.Dispose();
}

[SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
public static class StaticPools<T> where T : class, new()
{
    private static readonly IObjectPool<T> s_instance = StaticPools.Get<T>();
    public static T Rent() => s_instance.Rent();
    public static void Recycle(T instance) => s_instance.Recycle(instance);
    public static int RentedCount => s_instance.RentedCount;
    public static int PooledCount => s_instance.PooledCount;
    public static void Clear() => s_instance.Dispose();
}
