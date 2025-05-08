using System.Collections.Generic;

namespace Overlook.Pool;

public sealed class DefaultCollectionPoolProvider<TCollection, TElement> : IObjectPoolProvider where TCollection : class, ICollection<TElement>, new()
{
    public IObjectPool CreatePool() => new ObjectPool<TCollection, Policy>();

    private readonly record struct Policy : IObjectPoolPolicy
    {
        public object Create() => new TCollection();
        public void OnRent(object instance) => ((ICollection<TElement>)instance).Clear();
        public void OnRecycle(object instance) => ((ICollection<TElement>)instance).Clear();
        public void OnDispose(object instance) => ((ICollection<TElement>)instance).Clear();
    }
}
