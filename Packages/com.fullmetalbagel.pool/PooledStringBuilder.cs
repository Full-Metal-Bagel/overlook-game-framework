using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Pool;

namespace Game
{
    public readonly ref struct PooledStringBuilder
    {
#if !DISABLE_POOLED_COLLECTIONS_CHECKS
        private static readonly HashSet<object> s_usingCollections = new();
#endif

        private static readonly ObjectPool<StringBuilder> s_pool = new(createFunc: () => new StringBuilder(), actionOnRelease: s => s.Clear());
        private readonly StringBuilder _value;

        public PooledStringBuilder(int capacity)
        {
            _value = s_pool.Get();
            _value.Capacity = Math.Max(_value.Capacity, capacity);
#if !DISABLE_POOLED_COLLECTIONS_CHECKS
            if (!s_usingCollections.Add(_value))
                throw new PooledCollectionException("the collection had been occupied already");
#endif
        }

        public StringBuilder GetValue()
        {
#if !DISABLE_POOLED_COLLECTIONS_CHECKS
            if (!s_usingCollections.Contains(_value))
                throw new PooledCollectionException("the collection had been disposed already");
#endif
            return _value;
        }

        public static implicit operator StringBuilder(PooledStringBuilder self) => self.GetValue();
        public StringBuilder ToStringBuilder() => GetValue();

        public void Dispose()
        {
#if !DISABLE_POOLED_COLLECTIONS_CHECKS
            if (!s_usingCollections.Remove(_value))
                throw new PooledCollectionException("the collection had been disposed already");
#endif
            s_pool.Release(_value);
        }
    }
}
