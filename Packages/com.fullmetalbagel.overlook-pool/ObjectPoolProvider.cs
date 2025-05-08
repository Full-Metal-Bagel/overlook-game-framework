using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Overlook.Pool;

public interface IObjectPoolProvider
{
    IObjectPool CreatePool();
}

public static class ObjectPoolProvider
{
    // Cache to store policies by type
    private static readonly ConcurrentDictionary<Type, IObjectPoolProvider> s_providerCache = new();
    private static readonly IAssemblyObjectPoolProviderFactory[] s_factories =
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetCustomAttributes(false))
                .OfType<IAssemblyObjectPoolProviderFactory>()
                .OrderBy(item => item.Priority)
                .ToArray()
            ;

    public static IObjectPoolProvider Get(Type objectType)
    {
        return s_providerCache.GetOrAdd(objectType, static type =>
        {
            var attribute = type.GetCustomAttributes().SingleOrDefault(attribute => attribute is IObjectPoolProviderFactory);
            if (attribute != null) return ((IObjectPoolProviderFactory)attribute).CreateProvider(type)!;

            foreach (var factory in s_factories)
            {
                var provider = factory.CreateProvider(type);
                if (provider != null) return provider;
            }
            return (IObjectPoolProvider)Activator.CreateInstance(typeof(DefaultObjectPoolProvider<>).MakeGenericType(type));
        });
    }

    public static IObjectPoolProvider Get<T>() => Get(typeof(T));
}
