using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Overlook.Pool;

public static class SharedPools
{
    private static readonly TypePoolsCacheWithDefaultPolicy s_pools = new();

    public static IObjectPool Get(Type type)
    {
        WarnIfNoPoolAttributeAndMaxCountLessThan0(type);
        return s_pools.GetOrCreate(type);
    }

    public static IObjectPool<T> Get<T>() where T : class, new()
    {
        WarnIfNoPoolAttributeAndMaxCountLessThan0(typeof(T));
        return s_pools.GetOrCreate<T>();
    }

    [Conditional("OVERLOOK_DEBUG")]
    private static void WarnIfNoPoolAttributeAndMaxCountLessThan0(Type type)
    {
        if (!TypePoolsCacheWithDefaultPolicy.HasPoolPolicy(type))
            Debug.LogWarning($"No pool policy found for type {type.FullName}");
    }
}

[SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
public static class SharedPools<T> where T : class, new()
{
    public static IObjectPool<T> Instance { get; } = SharedPools.Get<T>();
    public static T Rent() => Instance.Rent();
    public static void Recycle(T instance) => Instance.Recycle(instance);
}
