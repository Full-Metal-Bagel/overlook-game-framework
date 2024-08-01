using System;
using System.Diagnostics.CodeAnalysis;

namespace RelEcs
{
    public struct WhereQuery<TEnumerable, TEnumerator, TQueryEntity> : IQuery<WhereQuery<TEnumerable, TEnumerator, TQueryEntity>.Enumerator, TQueryEntity>
        where TEnumerable : struct, IQuery<TEnumerator, TQueryEntity>
        where TEnumerator : struct, IQueryEnumerator<TQueryEntity>
        where TQueryEntity : IQueryEntity
    {
        [SuppressMessage("Style", "IDE0044:Add readonly modifier")]
        private TEnumerable _enumerable;
        private readonly Func<TQueryEntity, bool> _predicate;

        public WhereQuery(TEnumerable enumerable, Func<TQueryEntity, bool> predicate)
        {
            _predicate = predicate;
            _enumerable = enumerable;
        }

        public Enumerator GetEnumerator() => new(_enumerable.GetEnumerator(), _predicate);

        public WhereQuery<WhereQuery<TEnumerable, TEnumerator, TQueryEntity>, Enumerator, TQueryEntity> Where<T>(Func<T, bool> predicate) where T : unmanaged
        {
            return new WhereQuery<WhereQuery<TEnumerable, TEnumerator, TQueryEntity>, Enumerator, TQueryEntity>(this, entity => entity.Has<T>() && predicate(entity.Get<T>()));
        }

        public WhereQuery<WhereQuery<TEnumerable, TEnumerator, TQueryEntity>, Enumerator, TQueryEntity> WhereObject<T>(Func<T, bool> predicate) where T : class
        {
            return new WhereQuery<WhereQuery<TEnumerable, TEnumerator, TQueryEntity>, Enumerator, TQueryEntity>(this, entity => entity.Has<T>() && predicate(entity.GetObject<T>()));
        }

        public struct Enumerator : IQueryEnumerator<TQueryEntity>
        {
            [SuppressMessage("Style", "IDE0044:Add readonly modifier")]
            private TEnumerator _enumerator;
            private readonly Func<TQueryEntity, bool> _predicate;

            public Enumerator(TEnumerator enumerator, Func<TQueryEntity, bool> predicate)
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

            public TQueryEntity Current => _enumerator.Current;
        }
    }

    public static partial class ObjectComponentExtension
    {
        public static WhereQuery<WhereQuery<TEnumerable, TEnumerator, TQueryEntity>, WhereQuery<TEnumerable, TEnumerator, TQueryEntity>.Enumerator, TQueryEntity> Where<T, TEnumerable, TEnumerator, TQueryEntity>(this WhereQuery<TEnumerable, TEnumerator, TQueryEntity> query, Func<T, bool> predicate)
            where T : class
            where TEnumerable : struct, IQuery<TEnumerator, TQueryEntity>
            where TEnumerator : struct, IQueryEnumerator<TQueryEntity>
            where TQueryEntity : IQueryEntity
        {
            return query.WhereObject(predicate);
        }
    }
}
