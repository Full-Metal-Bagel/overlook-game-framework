using System;
using System.Text;

namespace Overlook.Pool;

[DisallowDefaultConstructor]
public readonly ref struct PooledStringBuilder
{
    public StringBuilder Value { get; }

    public PooledStringBuilder(int capacity)
    {
        Value = StaticPools<StringBuilder>.Rent();
        Value.Capacity = Math.Max(Value.Capacity, capacity);
    }

    public static implicit operator StringBuilder(PooledStringBuilder self) => self.Value;

    public void Dispose()
    {
        StaticPools<StringBuilder>.Recycle(Value);
    }
}
