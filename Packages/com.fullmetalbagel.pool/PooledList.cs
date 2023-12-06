using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace Game
{
    public sealed class PooledList<T> : IList<T>, IDisposable
    {
        private List<T> _collection = null!;

        public PooledList()
        {
            _collection = CollectionPool<List<T>, T>.Get();
        }

        public void Dispose()
        {
            CollectionPool<List<T>, T>.Release(_collection);
            _collection = null!;
        }

        public List<T>.Enumerator GetEnumerator() => _collection.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(T item) => _collection.Add(item);

        public void Clear() => _collection.Clear();

        public bool Contains(T item) => _collection.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _collection.CopyTo(array, arrayIndex);

        public bool Remove(T item) => _collection.Remove(item);

        public int Count => _collection.Count;

        public bool IsReadOnly => ((IList<T>)_collection).IsReadOnly;

        public int IndexOf(T item) => _collection.IndexOf(item);

        public void Insert(int index, T item) => _collection.Insert(index, item);

        public void RemoveAt(int index) => _collection.RemoveAt(index);

        public T this[int index]
        {
            get => _collection[index];
            set => _collection[index] = value;
        }
    }
}
