using System.Collections.Generic;

namespace Overlook.Pool;

public sealed class GenericCollectionPoolProvider<TCollection, TElement, TPolicy> : IObjectPoolProvider
    where TCollection : class, ICollection<TElement>, new()
    where TPolicy : unmanaged, IObjectPoolPolicy
{
    public IObjectPool CreatePool() => new ObjectPool<TCollection, TPolicy>();
}
