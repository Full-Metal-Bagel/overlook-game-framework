using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Overlook.Pool;

[SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
[DisallowDefaultConstructor]
public readonly ref struct PooledArray<T>
{
    public T[] Value { get; }

    public PooledArray(int minimumLength)
    {
        Value = ArrayPool<T>.Shared.Rent(minimumLength);
    }

    public static implicit operator T[](PooledArray<T> self) => self.Value;

    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(Value);
    }
}
