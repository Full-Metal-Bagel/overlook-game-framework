#nullable enable

using System;
using System.Collections.Generic;

namespace Overlook.Pool;

public sealed class PooledMemoryList<T> : IDisposable
{
    public List<T>? Value { get; private set; }

    public PooledMemoryList(int capacity)
    {
        Value = StaticPools<List<T>>.Rent();
        Value.Capacity = Math.Max(Value.Capacity, capacity);
    }

    public static implicit operator List<T>(PooledMemoryList<T> self) => self.Value!;

    public void Dispose()
    {
        if (Value != null) StaticPools<List<T>>.Recycle(Value);
        Value = null;
    }
}
