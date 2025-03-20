using System;
using System.Text;
using static Overlook.Pool.StaticPools;

namespace Overlook.Pool;

[DisallowDefaultConstructor]
public readonly ref struct PooledStringBuilder
{
    public StringBuilder Value { get; }

    public PooledStringBuilder(int capacity)
    {
        Value = GetPool<StringBuilder>().Rent();
        Value.Capacity = Math.Max(Value.Capacity, capacity);
    }

    public static implicit operator StringBuilder(PooledStringBuilder self) => self.Value;

    public void Dispose()
    {
        GetPool<StringBuilder>().Recycle(Value);
    }
}
