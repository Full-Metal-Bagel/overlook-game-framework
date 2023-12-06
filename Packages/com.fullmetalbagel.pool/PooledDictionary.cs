using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace Game
{
    public sealed class PooledDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable
    {
        private Dictionary<TKey, TValue> _collection = null!;

        public PooledDictionary()
        {
            _collection = CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>.Get();
        }

        public void Dispose()
        {
            CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>.Release(_collection);
            _collection = null!;
        }

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => _collection.GetEnumerator();
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(KeyValuePair<TKey, TValue> item) => _collection.Add(item.Key, item.Value);

        public void Clear() => _collection.Clear();

        public bool Contains(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)_collection).Contains(item);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary<TKey, TValue>)_collection).CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)_collection).Remove(item);

        public int Count => _collection.Count;

        public bool IsReadOnly => ((IDictionary<TKey, TValue>)_collection).IsReadOnly;

        public void Add(TKey key, TValue value) => _collection.Add(key, value);

        public bool ContainsKey(TKey key) => _collection.ContainsKey(key);

        public bool Remove(TKey key) => _collection.Remove(key);

        public bool TryGetValue(TKey key, out TValue value) => _collection.TryGetValue(key, out value);

        public TValue this[TKey key]
        {
            get => _collection[key];
            set => _collection[key] = value;
        }

        public ICollection<TKey> Keys => _collection.Keys;

        public ICollection<TValue> Values => _collection.Values;
    }
}
