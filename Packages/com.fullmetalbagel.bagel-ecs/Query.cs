using System;
using System.Collections.Generic;
using Game;
#if ARCHETYPE_USE_NATIVE_BIT_ARRAY
using TMask = RelEcs.NativeBitArrayMask;
#else
using TMask = RelEcs.Mask;
#endif

namespace RelEcs
{
    public interface IQuery<out TEnumerator, TQueryEntity> where TEnumerator : IQueryEnumerator<TQueryEntity>
    {
        TEnumerator GetEnumerator();
    }

    public interface IQueryEnumerator<TEntity>
    {
        bool MoveNext();
        TEntity Current { get; }
    }

    public readonly struct Query : IEquatable<Query>, IQuery<Query.Enumerator, QueryEntity>
    {
        internal List<Table> Tables { get; init; }
        internal Archetypes Archetypes { get; init; }
        internal TMask Mask { get; init; }

        internal static readonly Func<Archetypes, TMask, List<Table>, Query> s_createQuery =
            (archetypes, mask, matchingTables) => new Query { Archetypes = archetypes, Mask = mask, Tables = matchingTables };

        internal void AddTable(Table table)
        {
            Tables.Add(table);
        }

        public bool Contains(Entity entity)
        {
            var meta = Archetypes.GetEntityMeta(entity.Identity);
            var table = Archetypes.GetTable(meta.TableId);
            return Tables.Contains(table);
        }

        public bool Has(Entity entity, Type type)
        {
            return Archetypes.HasComponent(StorageType.Create(type), entity.Identity);
        }

        public bool Has<T>(Entity entity)
        {
            return Archetypes.HasComponent(StorageType.Create<T>(), entity.Identity);
        }

        public ref T Get<T>(Entity entity) where T : unmanaged
        {
            // Debug.Assert(Mask.HasTypesContainsAny(type => type == StorageType.Create<T>()));
            return ref Archetypes.GetComponent<T>(entity.Identity);
        }

        public T GetObject<T>(Entity entity) where T : class
        {
            // Debug.Assert(Mask.HasTypesContainsAny(type => type == StorageType.Create<T>() || typeof(T).IsAssignableFrom(type.Type)));
            return Archetypes.GetObjectComponent<T>(entity.Identity);
        }

        public void ForEach(Action<Entity> action)
        {
            foreach (var entity in this) action(entity);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public WhereQuery<Query, Enumerator, QueryEntity> Where<T>(Func<T, bool> predicate) where T : unmanaged
        {
            return new WhereQuery<Query, Enumerator, QueryEntity>(this, entity => entity.Has<T>() && predicate(entity.Get<T>()));
        }

        public WhereQuery<Query, Enumerator, QueryEntity> WhereObject<T>(Func<T, bool> predicate) where T : class
        {
            return new WhereQuery<Query, Enumerator, QueryEntity>(this, entity => entity.Has<T>() && predicate(entity.Get<T>()));
        }

        public QueryEntity Single()
        {
            using var enumerator = GetEnumerator();
            if (!enumerator.MoveNext()) throw new NoElementsException();
            var entity = enumerator.Current;
            if (enumerator.MoveNext()) throw new MoreThanOneElementsException();
            return entity;
        }

        public QueryEntity SingleOrDefault()
        {
            using var enumerator = GetEnumerator();
            if (!enumerator.MoveNext()) return new QueryEntity { Entity = Entity.None };
            var entity = enumerator.Current;
            if (enumerator.MoveNext()) throw new MoreThanOneElementsException();
            return entity;
        }

        public QueryEntity First()
        {
            using var enumerator = GetEnumerator();
            if (!enumerator.MoveNext()) throw new NoElementsException();
            return enumerator.Current;
        }

        public struct Enumerator : IQueryEnumerator<QueryEntity>, IDisposable
        {
            private readonly PooledMemoryList<QueryEntity> _cachedEntities;
            private List<QueryEntity>.Enumerator _enumerator;

            public Enumerator(in Query query)
            {
                _cachedEntities = new PooledMemoryList<QueryEntity>(128);
                foreach (var table in query.Tables)
                {
                    for (int i = 0; i < table.Count; i++)
                    {
                        var row = table.Rows[i];
                        var entity = table.GetStorage<Entity>()[row];
                        _cachedEntities.Add(new QueryEntity { Entity = entity, Query = query });
                    }
                }
                _enumerator = _cachedEntities.GetEnumerator();
            }

            public bool MoveNext() => _enumerator.MoveNext();
            public QueryEntity Current => _enumerator.Current;
            public void Dispose() => _cachedEntities.Dispose();
        }

        public bool Equals(Query other) => ReferenceEquals(Tables, other.Tables) && ReferenceEquals(Archetypes, other.Archetypes) && Mask.Equals(other.Mask);
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            throw new NotSupportedException();
        }

        public override int GetHashCode() => HashCode.Combine(Tables, Archetypes, Mask);
    }

    public static partial class ObjectComponentExtension
    {
        public static T Get<T>(this in Query query, Entity entity) where T : class
        {
            return query.GetObject<T>(entity);
        }

        public static T Get<T>(this in QueryEntity queryEntity) where T : class
        {
            return queryEntity.GetObject<T>();
        }

        public static WhereQuery<Query, Query.Enumerator, QueryEntity> Where<T>(this in Query query, Func<T, bool> predicate) where T : class
        {
            return query.WhereObject(predicate);
        }
    }

    public class NoElementsException : Exception
    {
        public NoElementsException() { }
        public NoElementsException(string message) : base(message) { }
        public NoElementsException(string message, Exception inner) : base(message, inner) { }
    }

    public class MoreThanOneElementsException : Exception
    {
        public MoreThanOneElementsException() { }
        public MoreThanOneElementsException(string message) : base(message) { }
        public MoreThanOneElementsException(string message, Exception inner) : base(message, inner) { }
    }
}
