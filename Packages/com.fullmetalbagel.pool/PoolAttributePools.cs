#nullable enable

using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Game;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PoolAttribute : Attribute
{
    public int InitCount { get; set; } = 0;
    public int MaxCount { get; set; } = int.MaxValue;
}

public sealed class PoolAttributePools : IDisposable
{
    private readonly Pools _pools = new();
    private static readonly ConcurrentDictionary<Type, PoolAttribute> s_cacheTypePoolAttributeMap = new();
    private static readonly PoolAttribute s_disabledPoolAttribute = new PoolAttribute { InitCount = 0, MaxCount = 0 };

    public static bool HasPoolAttribute(Type type) => !ReferenceEquals(s_disabledPoolAttribute, GetPoolAttribute(type));

    public IObjectPool<T> Get<T>(Action<T>? onRentAction = null, Action<T>? onRecycleAction = null, int? initCount = null, int? maxCount = null, Func<int, int>? expandFunc = null) where T : class, new()
    {
        var poolAttribute = GetPoolAttribute(typeof(T));
        return _pools.Get(onRentAction: onRentAction, onRecycleAction: onRecycleAction, initCount: initCount ?? poolAttribute.InitCount, maxCount: maxCount ?? poolAttribute.MaxCount, expandFunc: expandFunc);
    }

    public IObjectPool<T> Get<T>(Func<T> createFunc, Action<T>? onRentAction = null, Action<T>? onRecycleAction = null, int? initCount = null, int? maxCount = null, Func<int, int>? expandFunc = null) where T : class
    {
        var poolAttribute = GetPoolAttribute(typeof(T));
        return _pools.Get(createFunc: createFunc, onRentAction: onRentAction, onRecycleAction: onRecycleAction, initCount: initCount ?? poolAttribute.InitCount, maxCount: maxCount ?? poolAttribute.MaxCount, expandFunc: expandFunc);
    }

    public IObjectPool Get(Type type, int? initCount = null, int? maxCount = null, Func<int, int>? expandFunc = null)
    {
        var poolAttribute = GetPoolAttribute(type);
        return _pools.Get(type: type, initCount: initCount ?? poolAttribute.InitCount, maxCount: maxCount ?? poolAttribute.MaxCount, expandFunc: expandFunc);
    }

    public void Dispose() => _pools.Dispose();

    private static PoolAttribute GetPoolAttribute(Type type)
    {
        return s_cacheTypePoolAttributeMap.GetOrAdd(
            type,
            static type => type.GetCustomAttribute<PoolAttribute>() ?? s_disabledPoolAttribute
        );
    }
}
