using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Overlook.Ecs;

[SuppressMessage("Design", "CA1002:Do not expose generic lists")]
[DisallowDefaultConstructor]
internal readonly ref struct PooledList<T>
{
#if !DISABLE_OVERLOOK_POOLED_COLLECTIONS_CHECKS
    private static readonly HashSet<object> s_usingCollections = new();
#endif

    private readonly List<T> _value;

    public PooledList(int capacity)
    {
        _value = UnityEngine.Pool.ListPool<T>.Get();
        _value.Capacity = Math.Max(_value.Capacity, capacity);
#if !DISABLE_OVERLOOK_POOLED_COLLECTIONS_CHECKS
        if (!s_usingCollections.Add(_value))
            throw new PooledCollectionException("the collection had been occupied already");
#endif
    }

    public List<T> GetValue()
    {
#if !DISABLE_OVERLOOK_POOLED_COLLECTIONS_CHECKS
        if (!s_usingCollections.Contains(_value))
            throw new PooledCollectionException("the collection had been disposed already");
#endif
        return _value;
    }

    public static implicit operator List<T>(PooledList<T> self) => self.GetValue();
    public List<T> ToList() => GetValue();

    public void Dispose()
    {
#if !DISABLE_OVERLOOK_POOLED_COLLECTIONS_CHECKS
        if (!s_usingCollections.Remove(_value))
            throw new PooledCollectionException("the collection had been disposed already");
#endif
        UnityEngine.Pool.ListPool<T>.Release(_value);
    }

    public List<T>.Enumerator GetEnumerator() => GetValue().GetEnumerator();

    public void Add(T item) => GetValue().Add(item);

    public void Clear() => GetValue().Clear();

    public bool Contains(T item) => GetValue().Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => GetValue().CopyTo(array, arrayIndex);

    public bool Remove(T item) => GetValue().Remove(item);

    public int Count => GetValue().Count;

    public bool IsReadOnly => ((IList<T>)GetValue()).IsReadOnly;

    public int IndexOf(T item) => GetValue().IndexOf(item);

    public void Insert(int index, T item) => GetValue().Insert(index, item);

    public void RemoveAt(int index) => GetValue().RemoveAt(index);

    public T this[int index]
    {
        get => GetValue()[index];
        set => GetValue()[index] = value;
    }
}
