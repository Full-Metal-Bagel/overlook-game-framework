using System.Collections.Generic;

namespace Overlook.Pool;

[DisallowDefaultConstructor]
public readonly ref struct PooledHashSet<T>
{
    public HashSet<T> Value { get; }

    public PooledHashSet(int capacity)
    {
        Value = StaticPools<HashSet<T>>.Rent();
        Value.EnsureCapacity(capacity);
    }

    public static implicit operator HashSet<T>(PooledHashSet<T> self) => self.Value;

    public void Dispose()
    {
        StaticPools<HashSet<T>>.Recycle(Value);
    }
}
