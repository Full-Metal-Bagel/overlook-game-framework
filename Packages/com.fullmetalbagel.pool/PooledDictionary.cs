using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace Game
{
    public sealed class PooledDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable
    {
        public Dictionary<TKey, TValue> Value { get; private set; }

        public PooledDictionary()
        {
            Value = CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>.Get();
        }

        public static implicit operator Dictionary<TKey, TValue>(PooledDictionary<TKey, TValue> self) => self.Value;
        public Dictionary<TKey, TValue> ToDictionary() => Value;

        public void Dispose()
        {
            CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>.Release(Value);
            Value = null!;
        }

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => Value.GetEnumerator();
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(KeyValuePair<TKey, TValue> item) => Value.Add(item.Key, item.Value);

        public void Clear() => Value.Clear();

        public bool Contains(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)Value).Contains(item);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary<TKey, TValue>)Value).CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)Value).Remove(item);

        public int Count => Value.Count;

        public bool IsReadOnly => ((IDictionary<TKey, TValue>)Value).IsReadOnly;

        public void Add(TKey key, TValue value) => Value.Add(key, value);

        public bool ContainsKey(TKey key) => Value.ContainsKey(key);

        public bool Remove(TKey key) => Value.Remove(key);

        public bool TryGetValue(TKey key, out TValue value) => Value.TryGetValue(key, out value);

        public TValue this[TKey key]
        {
            get => Value[key];
            set => Value[key] = value;
        }

        public ICollection<TKey> Keys => Value.Keys;

        public ICollection<TValue> Values => Value.Values;
    }
}
