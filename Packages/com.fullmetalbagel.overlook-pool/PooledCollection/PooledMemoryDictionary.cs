using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static Overlook.Pool.StaticPools;

namespace Overlook.Pool;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public readonly record struct PooledMemoryDictionary<TKey, TValue> : IDisposable
{
    public Dictionary<TKey, TValue> Value { get; }

    public PooledMemoryDictionary(Dictionary<TKey, TValue> collection)
        : this(collection.Count)
    {
        foreach (var (key, value) in collection)
            Value!.Add(key, value);
    }

    public PooledMemoryDictionary() : this(0) { }

    public PooledMemoryDictionary(int capacity)
    {
        Value = GetPool<Dictionary<TKey, TValue>>().Rent();
        Value.EnsureCapacity(capacity);
    }

    public static implicit operator Dictionary<TKey, TValue>(PooledMemoryDictionary<TKey, TValue> self) => self.Value!;

    public void Dispose()
    {
        GetPool<Dictionary<TKey, TValue>>().Recycle(Value);
    }
}
