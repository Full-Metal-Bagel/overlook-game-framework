using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Game
{
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public readonly ref struct PooledArray<T>
    {
#if !DISABLE_POOLED_COLLECTIONS_CHECKS
        private static readonly HashSet<object> s_usingCollections = new();
#endif

        private readonly T[] _value;

        public T[] GetValue()
        {
#if !DISABLE_POOLED_COLLECTIONS_CHECKS
            if (!s_usingCollections.Contains(_value))
                throw new PooledCollectionException("the collection had been disposed already");
#endif
            return _value;
        }

        public int Count => GetValue().Length;
        public T this[int index] => GetValue()[index];

        public PooledArray(int minimumLength)
        {
            _value = ArrayPool<T>.Shared.Rent(minimumLength);
#if !DISABLE_POOLED_COLLECTIONS_CHECKS
            if (!s_usingCollections.Add(_value))
                throw new PooledCollectionException("the collection had been occupied already");
#endif
        }

        public static implicit operator T[](PooledArray<T> self) => self.GetValue();

        public T[] ToTArray() => GetValue();

        public void Dispose()
        {
#if !DISABLE_POOLED_COLLECTIONS_CHECKS
            if (!s_usingCollections.Remove(_value))
                throw new PooledCollectionException("the collection had been disposed already");
#endif
            ArrayPool<T>.Shared.Return(_value);
        }

        public Enumerator GetEnumerator() => new(GetValue());

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
