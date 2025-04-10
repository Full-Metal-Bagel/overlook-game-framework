using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Overlook.Pool;

public interface IObjectPoolProvider
{
    IObjectPool CreatePool();
}

public static class ObjectPoolProvider
{
    // Cache to store policies by type
    private static readonly ConcurrentDictionary<Type, IObjectPoolProvider> s_providerCache = new();
    private static readonly ThreadLocal<Type[]> s_typeParameters = new(() => new Type[1]);
    private static readonly

    static ObjectPoolProvider()
    {
        var globalProviderFactories = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetCustomAttributes(typeof(IObjectPoolProviderFactory), false))
            .OfType<IObjectPoolProviderFactory>()
        ;
        foreach (var attribute in ))
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
