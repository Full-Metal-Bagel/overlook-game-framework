using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.Pool;

namespace Game
{
    [SuppressMessage("Design", "CA1002:Do not expose generic lists")]
    public sealed class PooledList<T> : IList<T>, IDisposable
    {
        public List<T> Value { get; private set; }

        public PooledList()
        {
            Value = CollectionPool<List<T>, T>.Get();
        }

        public static implicit operator List<T>(PooledList<T> self) => self.Value;
        public List<T> ToList() => Value;

        public void Dispose()
        {
            CollectionPool<List<T>, T>.Release(Value);
            Value = null!;
        }

        public List<T>.Enumerator GetEnumerator() => Value.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(T item) => Value.Add(item);

        public void Clear() => Value.Clear();

        public bool Contains(T item) => Value.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => Value.CopyTo(array, arrayIndex);

        public bool Remove(T item) => Value.Remove(item);

        public int Count => Value.Count;

        public bool IsReadOnly => ((IList<T>)Value).IsReadOnly;

        public int IndexOf(T item) => Value.IndexOf(item);

        public void Insert(int index, T item) => Value.Insert(index, item);

        public void RemoveAt(int index) => Value.RemoveAt(index);

        public T this[int index]
        {
            get => Value[index];
            set => Value[index] = value;
        }
    }
}
