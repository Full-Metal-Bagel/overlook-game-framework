using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Overlook.Pool;

[SuppressMessage("Design", "CA1002:Do not expose generic lists")]
[DisallowDefaultConstructor]
public readonly ref struct PooledList<T>
{
    public List<T> Value { get; }

    public PooledList(int capacity)
    {
        Value = StaticPools<List<T>>.Rent();
        Value.Capacity = Math.Max(Value.Capacity, capacity);
    }

    public static implicit operator List<T>(PooledList<T> self) => self.Value;

    public void Dispose()
    {
        StaticPools<List<T>>.Recycle(Value);
    }
}
