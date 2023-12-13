using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Game
{
    public sealed class PooledArray<T> : IDisposable
    {
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
        public T[] Value { get; private set; }

        public PooledArray(int minimumLength)
        {
            Value = ArrayPool<T>.Shared.Rent(minimumLength);
        }

        public void Dispose()
        {
            ArrayPool<T>.Shared.Return(Value);
            Value = null!;
        }
    }
}
