using System.Collections.Generic;

namespace Game
{
    [DisallowDefaultConstructor]
    public readonly ref struct PooledHashSet<T>
    {
#if !DISABLE_POOLED_COLLECTIONS_CHECKS
        private static readonly HashSet<object> s_usingCollections = new();
#endif

        private readonly HashSet<T> _value;

        public PooledHashSet(int capacity)
        {
            _value = UnityEngine.Pool.HashSetPool<T>.Get();
            _value.EnsureCapacity(capacity);
#if !DISABLE_POOLED_COLLECTIONS_CHECKS
            if (!s_usingCollections.Add(_value))
                throw new PooledCollectionException("the collection had been occupied already");
#endif
        }

        public HashSet<T> GetValue()
        {
#if !DISABLE_POOLED_COLLECTIONS_CHECKS
            if (!s_usingCollections.Contains(_value))
                throw new PooledCollectionException("the collection had been disposed already");
#endif
            return _value;
        }

        public void Dispose()
        {
#if !DISABLE_POOLED_COLLECTIONS_CHECKS
            if (!s_usingCollections.Remove(_value))
                throw new PooledCollectionException("the collection had been disposed already");
#endif
            UnityEngine.Pool.HashSetPool<T>.Release(_value);
        }

        public HashSet<T>.Enumerator GetEnumerator() => GetValue().GetEnumerator();

        public void ExceptWith(IEnumerable<T> other) => GetValue().ExceptWith(other);

        public void IntersectWith(IEnumerable<T> other) => GetValue().IntersectWith(other);

        public bool IsProperSubsetOf(IEnumerable<T> other) => GetValue().IsProperSubsetOf(other);

        public bool IsProperSupersetOf(IEnumerable<T> other) => GetValue().IsProperSupersetOf(other);

        public bool IsSubsetOf(IEnumerable<T> other) => GetValue().IsSubsetOf(other);

        public bool IsSupersetOf(IEnumerable<T> other) => GetValue().IsSupersetOf(other);

        public bool Overlaps(IEnumerable<T> other) => GetValue().Overlaps(other);

        public bool SetEquals(IEnumerable<T> other) => GetValue().SetEquals(other);

        public void SymmetricExceptWith(IEnumerable<T> other) => GetValue().SymmetricExceptWith(other);

        public void UnionWith(IEnumerable<T> other) => GetValue().UnionWith(other);

        public bool Add(T item) => GetValue().Add(item);

        public void Clear() => GetValue().Clear();

        public bool Contains(T item) => GetValue().Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => GetValue().CopyTo(array, arrayIndex);

        public bool Remove(T item) => GetValue().Remove(item);

        public int Count => GetValue().Count;

        public bool IsReadOnly => ((ISet<T>)GetValue()).IsReadOnly;
    }
}
