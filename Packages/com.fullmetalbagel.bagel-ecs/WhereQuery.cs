using System;
using System.Diagnostics.CodeAnalysis;

namespace RelEcs
{
    public struct WhereQuery<TEnumerable, TEnumerator> : IQuery<WhereQuery<TEnumerable, TEnumerator>.Enumerator>
        where TEnumerable : struct, IQuery<TEnumerator>
        where TEnumerator : struct, IQueryEnumerator
    {
        [SuppressMessage("Style", "IDE0044:Add readonly modifier")]
        private TEnumerable _enumerable;
        private readonly Func<QueryEntity, bool> _predicate;

        public WhereQuery(TEnumerable enumerable, Func<QueryEntity, bool> predicate)
        {
            _predicate = predicate;
            _enumerable = enumerable;
        }

        public Enumerator GetEnumerator() => new(_enumerable.GetEnumerator(), _predicate);

        public WhereQuery<WhereQuery<TEnumerable, TEnumerator>, Enumerator> Where<T>(Func<T, bool> predicate) where T : struct
        {
            return new WhereQuery<WhereQuery<TEnumerable, TEnumerator>, Enumerator>(this, entity => entity.Has<T>() && predicate(entity.Get<T>()));
        }

        public WhereQuery<WhereQuery<TEnumerable, TEnumerator>, Enumerator> WhereObject<T>(Func<T, bool> predicate) where T : class
        {
            return new WhereQuery<WhereQuery<TEnumerable, TEnumerator>, Enumerator>(this, entity => entity.Has<T>() && predicate(entity.Get<T>()));
        }

        public struct Enumerator : IQueryEnumerator
        {
            [SuppressMessage("Style", "IDE0044:Add readonly modifier")]
            private TEnumerator _enumerator;
            private readonly Func<QueryEntity, bool> _predicate;

            public Enumerator(TEnumerator enumerator, Func<QueryEntity, bool> predicate)
            {
                _enumerator = enumerator;
                _predicate = predicate;
            }

            public bool MoveNext()
            {
                while (_enumerator.MoveNext())
                {
                    var current = _enumerator.Current;
                    if (_predicate(current)) return true;
                }
                return false;
            }

            public QueryEntity Current => _enumerator.Current;
        }
    }

    public static partial class ObjectComponentExtension
    {
        public static WhereQuery<WhereQuery<TEnumerable, TEnumerator>, WhereQuery<TEnumerable, TEnumerator>.Enumerator> Where<T, TEnumerable, TEnumerator>(this WhereQuery<TEnumerable, TEnumerator> query, Func<T, bool> predicate)
            where T : class
            where TEnumerable : struct, IQuery<TEnumerator>
            where TEnumerator : struct, IQueryEnumerator
        {
            return query.WhereObject(predicate);
        }
    }
}
