using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Game
{
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public sealed class PooledArray<T> : IReadOnlyList<T>, IDisposable
    {
        public T[] Value { get; private set; }
        public int Count => Value.Length;
        public T this[int index] => Value[index];

        public PooledArray(int minimumLength)
        {
            Value = ArrayPool<T>.Shared.Rent(minimumLength);
        }

        public static implicit operator T[](PooledArray<T> self) => self.Value;
        public T[] ToTArray() => Value;

        public void Dispose()
        {
            ArrayPool<T>.Shared.Return(Value);
            Value = null!;
        }

        public Enumerator GetEnumerator() => new(Value);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Enumerator struct
        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[] _array;
            private int _index;

            public Enumerator(T[] array)
            {
                _array = array;
                _index = -1;
            }

            public T Current => _array[_index];

            object IEnumerator.Current => Current!;

            public bool MoveNext()
            {
                return ++_index < _array.Length;
            }

            public void Reset()
            {
                _index = -1;
            }

            public void Dispose() { }
        }
    }
}
