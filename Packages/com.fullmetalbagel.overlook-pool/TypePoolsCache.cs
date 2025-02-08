#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Overlook.Pool;

[SuppressMessage("Naming", "CA1724:The type name Pools conflicts in whole or in part with the namespace name 'FluffyUnderware.Curvy.Pools'")]
public sealed class TypePoolsCache : IDisposable
{
    private readonly ConcurrentDictionary<Type, IObjectPool> _pools = new();

    public IObjectPool GetOrCreate(Type type, int initCount = 0, int maxCount = int.MaxValue, Func<int, int>? expandFunc = null)
    {
        return _pools.GetOrAdd(type, static (type, t) => CreatePool(type: type, initCount: t.initCount, maxCount: t.maxCount, expandFunc: t.expandFunc), (initCount, maxCount, expandFunc));
    }

    public IObjectPool<T> GetOrCreate<T>(Func<T> createFunc, Action<T>? onRentAction = null, Action<T>? onRecycleAction = null, int initCount = 0, int maxCount = int.MaxValue, Func<int, int>? expandFunc = null) where T : class
    {
        return (IObjectPool<T>)_pools.GetOrAdd
        (
            typeof(T),
            static (_, t) => CreateTypedPool(createFunc: t.createFunc, onRentAction: t.onRentAction, onRecycleAction: t.onRecycleAction, initCount: t.initCount, maxCount: t.maxCount, expandFunc: t.expandFunc),
            (createFunc, onRentAction, onRecycleAction, initCount, maxCount, expandFunc)
        );
    }

    public IObjectPool<T> GetOrCreate<T>(Action<T>? onRentAction = null, Action<T>? onRecycleAction = null, int initCount = 0, int maxCount = int.MaxValue, Func<int, int>? expandFunc = null) where T : class, new()
    {
        return GetOrCreate(createFunc: static () => new T(), onRentAction: onRentAction, onRecycleAction: onRecycleAction, initCount: initCount, maxCount: maxCount, expandFunc: expandFunc);
    }

    public void Dispose()
    {
        foreach (var pool in _pools.Values)
        {
            if (pool is IDisposable disposable) disposable.Dispose();
        }
        _pools.Clear();
    }

    private static IObjectPool CreatePool(Type type, int initCount, int maxCount, Func<int, int>? expandFunc)
    {
        // TODO: optimize reflection?
        var createMethod = typeof(TypePoolsCache).GetMethod(nameof(CreateTypedPoolWithNewInstance), BindingFlags.NonPublic | BindingFlags.Static);
        try
        {
            var genericMethod = createMethod.MakeGenericMethod(type);
            return (IObjectPool)genericMethod.Invoke(null, new object?[] { null, null, initCount, maxCount, expandFunc });
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"{type.Name} must have a parameterless constructor for creating corresponding `ObjectPool` from type", nameof(type), ex);
        }
    }

    private static ObjectPool<T> CreateTypedPool<T>(Func<T> createFunc, Action<T>? onRentAction, Action<T>? onRecycleAction, int initCount, int maxCount, Func<int, int>? expandFunc) where T : class
    {
        return new ObjectPool<T>(
            createFunc: createFunc,
            onRentAction: onRentAction,
            onRecycleAction: onRecycleAction,
            initCount: initCount,
            maxCount: maxCount,
            expandFunc: expandFunc
        );
    }

    private static ObjectPool<T> CreateTypedPoolWithNewInstance<T>(Action<T>? onRentAction, Action<T>? onRecycleAction, int initCount, int maxCount, Func<int, int>? expandFunc) where T : class, new()
    {
        return CreateTypedPool(
            createFunc: static () => new T(),
            onRentAction: onRentAction,
            onRecycleAction: onRecycleAction,
            initCount: initCount,
            maxCount: maxCount,
            expandFunc: expandFunc
        );
    }
}
