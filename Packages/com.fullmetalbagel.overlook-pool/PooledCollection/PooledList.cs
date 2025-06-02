using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static Overlook.Pool.StaticPools;

namespace Overlook.Pool;

[SuppressMessage("Design", "CA1002:Do not expose generic lists")]
public readonly ref struct PooledList<T>
{
    public List<T> Value { get; }

    public PooledList() : this(0) { }

    public PooledList(int capacity)
    {
        Value = GetPool<List<T>>().Rent();
        Value.Capacity = Math.Max(Value.Capacity, capacity);
    }

    public static implicit operator List<T>(PooledList<T> self) => self.Value;

    public void Dispose()
    {
        GetPool<List<T>>().Recycle(Value);
    }
}
