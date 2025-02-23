using System;
using System.Collections.Generic;
using System.Text;

namespace Overlook.Pool;

[DisallowDefaultConstructor]
public readonly ref struct PooledStringBuilder
{
#if !DISABLE_OVERLOOK_POOLED_COLLECTIONS_CHECKS
    private static readonly HashSet<object> s_usingCollections = new();
#endif

    private readonly StringBuilder _value;

    public PooledStringBuilder(int capacity)
    {
        _value = SharedPools<StringBuilder>.Rent();
        _value.Capacity = Math.Max(_value.Capacity, capacity);
#if !DISABLE_OVERLOOK_POOLED_COLLECTIONS_CHECKS
        if (!s_usingCollections.Add(_value))
            throw new PooledCollectionException("the collection had been occupied already");
#endif
    }

    public StringBuilder GetValue()
    {
#if !DISABLE_OVERLOOK_POOLED_COLLECTIONS_CHECKS
        if (!s_usingCollections.Contains(_value))
            throw new PooledCollectionException("the collection had been disposed already");
#endif
        return _value;
    }

    public static implicit operator StringBuilder(PooledStringBuilder self) => self.GetValue();
    public StringBuilder ToStringBuilder() => GetValue();

    public void Dispose()
    {
#if !DISABLE_OVERLOOK_POOLED_COLLECTIONS_CHECKS
        if (!s_usingCollections.Remove(_value))
            throw new PooledCollectionException("the collection had been disposed already");
#endif
        SharedPools<StringBuilder>.Recycle(_value);
    }
}
