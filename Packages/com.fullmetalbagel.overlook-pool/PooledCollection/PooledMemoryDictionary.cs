using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Overlook.Pool;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
[DisallowDefaultConstructor]
public readonly record struct PooledMemoryDictionary<TKey, TValue> : IDisposable
{
    public Dictionary<TKey, TValue> Value { get; }

    public PooledMemoryDictionary(Dictionary<TKey, TValue> collection)
        : this(collection.Count)
    {
        foreach (var (key, value) in collection)
            Value!.Add(key, value);
    }

    public PooledMemoryDictionary(int capacity)
    {
        Value = StaticPools<Dictionary<TKey, TValue>>.Rent();
        Value.EnsureCapacity(capacity);
    }

    public static implicit operator Dictionary<TKey, TValue>(PooledMemoryDictionary<TKey, TValue> self) => self.Value!;

    public void Dispose()
    {
        StaticPools<Dictionary<TKey, TValue>>.Recycle(Value);
    }
}
