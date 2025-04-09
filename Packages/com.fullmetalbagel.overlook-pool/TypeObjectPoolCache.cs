using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Overlook.Pool;

public sealed class TypeObjectPoolCache : IDisposable
{
    private readonly ConcurrentDictionary<Type, IObjectPool> _pools = new();

    public IObjectPool GetPool(Type type, IObjectPoolProvider? provider = null)
    {
        provider ??= DefaultObjectPoolProvider.Get(type);
        CheckProvider(type, provider);
        return _pools.GetOrAdd(type, static (_, provider) => provider.CreatePool(), provider);
    }

    public IObjectPool<T> GetPool<T>(IObjectPoolProvider? provider = null) where T : class, new()
    {
        provider ??= DefaultObjectPoolProvider.Get<T>();
        CheckProvider(typeof(T), provider);
        return (IObjectPool<T>)_pools.GetOrAdd(typeof(T), static (_, provider) => provider.CreatePool(), provider);
    }

    public void Dispose()
    {
        foreach (var pool in _pools.Values) pool.Dispose();
        _pools.Clear();
    }

#if OVERLOOK_DEBUG
    private readonly ConcurrentDictionary<Type, IObjectPoolProvider> _providers = new();
#endif

    [Conditional("OVERLOOK_DEBUG")]
    private void CheckProvider(Type type, IObjectPoolProvider provider)
    {
#if OVERLOOK_DEBUG
        if (_providers.TryGetValue(type, out IObjectPoolProvider originalProvider))
        {
            if (!ReferenceEquals(originalProvider, provider))
                throw new ProviderNotMatchException();
        }
        else
        {
            _providers.TryAdd(type, provider);
        }
#endif
    }
}

public class ProviderNotMatchException : Exception
{
    public ProviderNotMatchException() { }
    public ProviderNotMatchException(string message) : base(message) { }
    public ProviderNotMatchException(string message, Exception inner) : base(message, inner) { }
}
