using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Overlook.Pool;

[SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations")]
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
[DisallowDefaultConstructor]
public readonly struct PooledMemoryDictionary<TKey, TValue> : IDisposable
{
#if !DISABLE_OVERLOOK_POOLED_COLLECTIONS_CHECKS
    private static readonly HashSet<object> s_usingCollections = new();
#endif

    private readonly Dictionary<TKey, TValue> _value;

    public PooledMemoryDictionary(Dictionary<TKey, TValue> collection)
        : this(collection.Count)
    {
        foreach (var (key, value) in collection)
            _value.Add(key, value);
    }

    public PooledMemoryDictionary(int capacity)
    {
        _value = UnityEngine.Pool.DictionaryPool<TKey, TValue>.Get();
        _value.EnsureCapacity(capacity);
#if !DISABLE_OVERLOOK_POOLED_COLLECTIONS_CHECKS
        if (!s_usingCollections.Add(_value))
            throw new PooledCollectionException("the collection had been occupied already");
#endif
    }

    public Dictionary<TKey, TValue> GetValue()
    {
#if !DISABLE_OVERLOOK_POOLED_COLLECTIONS_CHECKS
        if (!s_usingCollections.Contains(_value))
            throw new PooledCollectionException("the collection had been disposed already");
#endif
        return _value;
    }

    public static implicit operator Dictionary<TKey, TValue>(PooledMemoryDictionary<TKey, TValue> self) => self.GetValue();
    public Dictionary<TKey, TValue> ToDictionary() => GetValue();

    public void Dispose()
    {
#if !DISABLE_OVERLOOK_POOLED_COLLECTIONS_CHECKS
        if (!s_usingCollections.Remove(_value))
            throw new PooledCollectionException("the collection had been disposed already");
#endif
        UnityEngine.Pool.DictionaryPool<TKey, TValue>.Release(_value);
    }

    public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => GetValue().GetEnumerator();

    public void Add(KeyValuePair<TKey, TValue> item) => GetValue().Add(item.Key, item.Value);

    public void Clear() => GetValue().Clear();

    public bool Contains(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)GetValue()).Contains(item);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary<TKey, TValue>)GetValue()).CopyTo(array, arrayIndex);

    public bool Remove(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)GetValue()).Remove(item);

    public int Count => GetValue().Count;

    public bool IsReadOnly => ((IDictionary<TKey, TValue>)GetValue()).IsReadOnly;

    public void Add(TKey key, TValue value) => GetValue().Add(key, value);

    public bool ContainsKey(TKey key) => GetValue().ContainsKey(key);

    public bool Remove(TKey key) => GetValue().Remove(key);

    public bool TryGetValue(TKey key, out TValue value) => GetValue().TryGetValue(key, out value);

    public TValue this[TKey key]
    {
        get => GetValue()[key];
        set => GetValue()[key] = value;
    }

    public ICollection<TKey> Keys => GetValue().Keys;

    public ICollection<TValue> Values => GetValue().Values;
}
