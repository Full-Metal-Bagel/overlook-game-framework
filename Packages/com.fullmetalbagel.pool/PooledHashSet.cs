using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace Game
{
    public sealed class PooledHashSet<T> : ISet<T>, IDisposable
    {
        private HashSet<T> _collection = null!;

        public PooledHashSet()
        {
            _collection = CollectionPool<HashSet<T>, T>.Get();
        }

        public void Dispose()
        {
            CollectionPool<HashSet<T>, T>.Release(_collection);
            _collection = null!;
        }

        public HashSet<T>.Enumerator GetEnumerator() => _collection.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection<T>.Add(T item) => _collection.Add(item);

        public void ExceptWith(IEnumerable<T> other) => _collection.ExceptWith(other);

        public void IntersectWith(IEnumerable<T> other) => _collection.IntersectWith(other);

        public bool IsProperSubsetOf(IEnumerable<T> other) => _collection.IsProperSubsetOf(other);

        public bool IsProperSupersetOf(IEnumerable<T> other) => _collection.IsProperSupersetOf(other);

        public bool IsSubsetOf(IEnumerable<T> other) => _collection.IsSubsetOf(other);

        public bool IsSupersetOf(IEnumerable<T> other) => _collection.IsSupersetOf(other);

        public bool Overlaps(IEnumerable<T> other) => _collection.Overlaps(other);

        public bool SetEquals(IEnumerable<T> other) => _collection.SetEquals(other);

        public void SymmetricExceptWith(IEnumerable<T> other) => _collection.SymmetricExceptWith(other);

        public void UnionWith(IEnumerable<T> other) => _collection.UnionWith(other);

        public bool Add(T item) => _collection.Add(item);

        public void Clear() => _collection.Clear();

        public bool Contains(T item) => _collection.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _collection.CopyTo(array, arrayIndex);

        public bool Remove(T item) => _collection.Remove(item);

        public int Count => _collection.Count;

        public bool IsReadOnly => ((ISet<T>)_collection).IsReadOnly;
    }
}
