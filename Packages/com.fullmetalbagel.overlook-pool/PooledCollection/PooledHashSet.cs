using System.Collections.Generic;
using static Overlook.Pool.StaticPools;

namespace Overlook.Pool;

public readonly ref struct PooledHashSet<T>
{
    public HashSet<T> Value { get; }

    public PooledHashSet() : this(0) { }

    public PooledHashSet(int capacity)
    {
        Value = GetPool<HashSet<T>>().Rent();
        Value.EnsureCapacity(capacity);
    }

    public static implicit operator HashSet<T>(PooledHashSet<T> self) => self.Value;

    public void Dispose()
    {
        GetPool<HashSet<T>>().Recycle(Value);
    }
}
