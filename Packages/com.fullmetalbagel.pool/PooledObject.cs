using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Game
{
    public readonly ref struct PooledObject<T> where T : class, new()
    {
#if !DISABLE_POOLED_COLLECTIONS_CHECKS
        private static readonly ConcurrentDictionary<object, object> s_usingCollections = new();
#endif

        private static readonly ThreadLocal<List<T>> s_pool = new(() => new(32));
        private readonly T _value;

        public PooledObject(int _)
        {
            var pool = s_pool.Value;
            if (pool.Count > 0)
            {
                var index = pool.Count - 1;
                _value = pool[index];
                pool.RemoveAt(index);
            }
            else
            {
                _value = new T();
            }
#if !DISABLE_POOLED_COLLECTIONS_CHECKS
            if (!s_usingCollections.TryAdd(_value, _value))
                throw new PooledCollectionException("the collection had been occupied already");
#endif
        }

        public T GetValue()
        {
#if !DISABLE_POOLED_COLLECTIONS_CHECKS
            if (!s_usingCollections.ContainsKey(_value))
                throw new PooledCollectionException("the collection had been disposed already");
#endif
            return _value;
        }

        [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates")]
        public static implicit operator T(PooledObject<T> self) => self.GetValue();

        public void Dispose()
        {
#if !DISABLE_POOLED_COLLECTIONS_CHECKS
            if (!s_usingCollections.TryRemove(_value, out _))
                throw new PooledCollectionException("the collection had been disposed already");
#endif
            s_pool.Value.Add(_value);
        }
    }
}
