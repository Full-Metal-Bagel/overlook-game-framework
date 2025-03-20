using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Overlook.Pool;

public interface IObjectPoolPolicy
{
    int InitCount { get; }
    int MaxCount { get; }
    int Expand(int size);
    object Create();
}

public sealed class DefaultObjectPoolPolicy<T> : IObjectPoolPolicy where T : class, new()
{
    public int InitCount { get; init; } = 1;
    public int MaxCount { get; init; } = int.MaxValue;
    public int Expand(int size) => size * 2;
    public object Create() => new T();
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class PoolPolicyAttribute<TPolicy> : Attribute where TPolicy : IObjectPoolPolicy
{
}

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class PoolPolicyAttribute<TPooledObject, TPolicy> : Attribute
    where TPooledObject : class
    where TPolicy : IObjectPoolPolicy
{
}

public static class StaticObjectPoolPolicy
{
    // Cache to store policies by type
    private static readonly ConcurrentDictionary<Type, IObjectPoolPolicy> s_policyCache = new();
    private static readonly IReadOnlyDictionary<Type, Type> s_assemblyPolicies =
        AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetCustomAttributes(typeof(PoolPolicyAttribute<,>), false))
            .Select(attribute => attribute.GetType().GetGenericArguments())
            .ToDictionary(g => g[0], g => g[1])
        ;

    public static IObjectPoolPolicy Get(Type type)
    {
        return s_policyCache.GetOrAdd(type, CreatePolicy);
    }

    public static IObjectPoolPolicy Get<T>() where T : class, new()
    {
        return s_policyCache.GetOrAdd(typeof(T), _ => CreatePolicy<T>());
    }

    private static IObjectPoolPolicy CreatePolicy<T>() where T : class, new()
    {
        var policyType = FindPolicyTypeFromAttribute(typeof(T));
        if (policyType == null) return new DefaultObjectPoolPolicy<T>();
        return (IObjectPoolPolicy)Activator.CreateInstance(policyType);
    }

    private static IObjectPoolPolicy CreatePolicy(Type objectType)
    {
        var policyType = FindPolicyTypeFromAttribute(objectType) ?? typeof(DefaultObjectPoolPolicy<>).MakeGenericType(objectType);
        return (IObjectPoolPolicy)Activator.CreateInstance(policyType);
    }

    private static Type? FindPolicyTypeFromAttribute(Type objectType)
    {
        Type? policyType = null;
        var classAttribute = objectType.GetCustomAttribute(typeof(PoolPolicyAttribute<>), false);
        if (classAttribute != null) policyType = classAttribute.GetType().GetGenericArguments()[0];
        if (policyType == null) s_assemblyPolicies.TryGetValue(objectType, out policyType);
        return policyType;
    }
}
