#nullable enable

using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Overlook.Pool;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PoolAttribute : Attribute
{
    public Type? DefaultPolicy { get; init; }
}

public sealed class TypePoolsCacheWithDefaultPolicy : IDisposable
{
    private readonly TypePoolsCache _pools = new();
    private static readonly ConcurrentDictionary<Type, IObjectPoolPolicy> s_cacheTypePoolPolicy = new();

    public static bool HasPoolPolicy(Type type) => s_cacheTypePoolPolicy.ContainsKey(type);

    public IObjectPool<T> GetOrCreate<T>() where T : class, new()
    {
        var policy = s_cacheTypePoolPolicy.GetOrAdd(
            typeof(T),
            static _ => GetOrCreatePoolPolicy<T>()
        );
        return _pools.GetOrCreate<T>(policy);
    }

    public IObjectPool GetOrCreate(Type type)
    {
        var policy = s_cacheTypePoolPolicy.GetOrAdd(
            type,
            static type => GetOrCreatePoolPolicy(type)
        );
        return _pools.GetOrCreate(type, policy);
    }

    public void Dispose() => _pools.Dispose();

    private static IObjectPoolPolicy GetOrCreatePoolPolicy(Type type)
    {
        var policyType = type.GetCustomAttribute<PoolAttribute>()?.DefaultPolicy ?? typeof(DefaultObjectPoolPolicy<>).MakeGenericType(type);
        return (IObjectPoolPolicy)policyType.GetConstructor(Array.Empty<Type>())!.Invoke(Array.Empty<object>());
    }

    private static IObjectPoolPolicy GetOrCreatePoolPolicy<T>() where T : class, new()
    {
        var policyType = typeof(T).GetCustomAttribute<PoolAttribute>()?.DefaultPolicy;
        return policyType is null ? new DefaultObjectPoolPolicy<T>() : GetOrCreatePoolPolicy(policyType);
    }
}
