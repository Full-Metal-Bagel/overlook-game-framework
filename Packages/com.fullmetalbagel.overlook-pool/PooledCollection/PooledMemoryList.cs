#nullable enable

using System;
using System.Collections.Generic;

namespace Overlook.Pool;

[DisallowDefaultConstructor]
public readonly record struct PooledMemoryList<T> : IDisposable
{
    public List<T> Value { get; }

    public PooledMemoryList(int capacity)
    {
        Value = StaticPools<List<T>>.Rent();
        Value.Capacity = Math.Max(Value.Capacity, capacity);
    }

    public static implicit operator List<T>(PooledMemoryList<T> self) => self.Value;

    public void Dispose()
    {
        StaticPools<List<T>>.Recycle(Value);
    }
}
