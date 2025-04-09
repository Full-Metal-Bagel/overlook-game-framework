using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Overlook.Pool;

public interface IObjectPoolProvider
{
    Type ObjectType { get; }
    IObjectPool CreatePool();
}

public sealed class DefaultObjectPoolProvider<T> : IObjectPoolProvider where T : class, new()
{
    public Type ObjectType => typeof(T);
    public IObjectPool CreatePool() => new DefaultObjectPool<T, DefaultObjectPoolPolicy<T>>();
}

public sealed class DefaultCollectionPoolProvider<TCollection, TElement> : IObjectPoolProvider where TCollection : class, ICollection<TElement>, new()
{
    public Type ObjectType => typeof(TCollection);
    public IObjectPool CreatePool() => new DefaultObjectPool<TCollection, DefaultCollectionPoolPolicy<TCollection, TElement>>();
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class RegisterDefaultObjectPoolAttribute<T, TPolicy> : Attribute, IObjectPoolProvider
    where T : class
    where TPolicy : unmanaged, IObjectPoolPolicy
{
    public Type ObjectType => typeof(T);
    public IObjectPool CreatePool() => new DefaultObjectPool<T, TPolicy>();
}

public static class DefaultObjectPoolProvider
{
    // Cache to store policies by type
    private static readonly ConcurrentDictionary<Type, IObjectPoolProvider> s_providerCache = new();
    private static readonly ThreadLocal<Type[]> s_typeParameters = new(() => new Type[1]);

    static DefaultObjectPoolProvider()
    {
        foreach (var attribute in AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetCustomAttributes(typeof(RegisterDefaultObjectPoolAttribute<,>), false)))
        {
            var objectType = attribute.GetType().GenericTypeArguments[0];
            s_providerCache.TryAdd(objectType, (IObjectPoolProvider)attribute);
        }
    }

    public static IObjectPoolProvider Get(Type objectType)
    {
        return s_providerCache.GetOrAdd(objectType, static type =>
        {
            s_typeParameters.Value[0] = type;
            return (IObjectPoolProvider)Activator.CreateInstance(typeof(DefaultObjectPoolProvider<>).MakeGenericType(s_typeParameters.Value));
        });
    }

    public static IObjectPoolProvider Get<T>() where T : class, new()
    {
        return s_providerCache.GetOrAdd(typeof(T), static _ => new DefaultObjectPoolProvider<T>());
    }
}
