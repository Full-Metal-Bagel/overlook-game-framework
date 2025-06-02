#nullable enable

using System;
using System.Collections.Generic;
using static Overlook.Pool.StaticPools;

namespace Overlook.Pool;

public readonly record struct PooledMemoryList<T> : IDisposable
{
    public List<T> Value { get; }

    public PooledMemoryList() : this(0) { }

    public PooledMemoryList(int capacity)
    {
        Value = GetPool<List<T>>().Rent();
        Value.Capacity = Math.Max(Value.Capacity, capacity);
    }

    public static implicit operator List<T>(PooledMemoryList<T> self) => self.Value;

    public void Dispose()
    {
        GetPool<List<T>>().Recycle(Value);
    }
}
