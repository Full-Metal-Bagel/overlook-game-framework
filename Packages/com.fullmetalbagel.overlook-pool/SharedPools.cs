using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Overlook.Pool;

public static class SharedPools
{
    private static readonly PoolAttributeTypePoolsCache s_pools = new();

    public static IObjectPool Get(Type type, int? initCount = null, int? maxCount = null, Func<int, int>? expandFunc = null)
    {
        WarnIfNoPoolAttributeAndMaxCountLessThan0(type, maxCount);
        return s_pools.GetOrCreate(type: type, initCount: initCount, maxCount: maxCount, expandFunc: expandFunc);
    }

    public static IObjectPool<T> Get<T>(Action<T>? onRentAction = null, Action<T>? onRecycleAction = null, int? initCount = null, int? maxCount = null, Func<int, int>? expandFunc = null) where T : class, new()
    {
        WarnIfNoPoolAttributeAndMaxCountLessThan0(typeof(T), maxCount);
        return s_pools.GetOrCreate(initCount: initCount, onRentAction: onRentAction, onRecycleAction: onRecycleAction, maxCount: maxCount, expandFunc: expandFunc);
    }

    public static IObjectPool<T> Get<T>(Func<T> createFunc, Action<T>? onRentAction = null, Action<T>? onRecycleAction = null, int? initCount = null, int? maxCount = null, Func<int, int>? expandFunc = null) where T : class
    {
        WarnIfNoPoolAttributeAndMaxCountLessThan0(typeof(T), maxCount);
        return s_pools.GetOrCreate(createFunc: createFunc, onRentAction: onRentAction, onRecycleAction: onRecycleAction, initCount: initCount, maxCount: maxCount, expandFunc: expandFunc);
    }

    [Conditional("KG_DEBUG")]
    private static void WarnIfNoPoolAttributeAndMaxCountLessThan0(Type type, int? maxCount)
    {
        if (maxCount is null or <= 0 && !PoolAttributeTypePoolsCache.HasPoolAttribute(type))
            Debug.LogWarning($"No pool attribute found for type {type.FullName}");
    }
}

[SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
public static class SharedPools<T> where T : class, new()
{
    public static IObjectPool<T> Instance { get; } = SharedPools.Get<T>();
    public static T Rent() => Instance.Rent();
    public static void Recycle(T instance) => Instance.Recycle(instance);
}
