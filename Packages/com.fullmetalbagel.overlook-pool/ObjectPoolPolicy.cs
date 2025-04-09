using System;
using System.Collections.Generic;
using System.Text;

namespace Overlook.Pool;

public interface IObjectPoolPolicy
{
    object Create();
    public int InitCount => 1;
    public int MaxCount => int.MaxValue;
    public int Expand(int size) => size * 2;
    void OnRent(object instance) => (instance as IObjectPoolCallback)?.OnRent();
    void OnRecycle(object instance) => (instance as IObjectPoolCallback)?.OnRecycle();
    void OnDispose(object instance) => (instance as IDisposable)?.Dispose();
}

public readonly record struct DefaultObjectPoolPolicy<T> : IObjectPoolPolicy where T : class, new()
{
    public object Create() => new T();
}

public readonly record struct DefaultCollectionPoolPolicy<TCollection, TElement> : IObjectPoolPolicy where TCollection : class, ICollection<TElement>, new()
{
    public object Create() => new TCollection();
    public void OnRecycle(object instance) => ((ICollection<TElement>)instance).Clear();
    public void OnDispose(object instance) => ((ICollection<TElement>)instance).Clear();
}

public readonly record struct DefaultStringBuilderPoolPolicy : IObjectPoolPolicy
{
    public object Create() => new StringBuilder();
    public void OnRecycle(object instance) => ((StringBuilder)instance).Clear();
    public void OnDispose(object instance) => ((StringBuilder)instance).Clear();
}
