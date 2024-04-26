using System;
using System.Collections.Generic;
#if ARCHETYPE_USE_NATIVE_BIT_ARRAY
using TMask = RelEcs.NativeBitArrayMask;
#else
using TMask = RelEcs.Mask;
#endif

namespace RelEcs
{
    public interface IQuery<out TEnumerator> where TEnumerator : IQueryEnumerator
    {
        TEnumerator GetEnumerator();
    }

    public interface IQueryEnumerator
    {
        bool MoveNext();
        QueryEntity Current { get; }
    }

    public readonly struct Query : IEquatable<Query>, IQuery<Query.Enumerator>
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

        public ref T Get<T>(Entity entity) where T : struct
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

        public WhereQuery<Query, Enumerator> Where<T>(Func<T, bool> predicate) where T : struct
        {
            return new WhereQuery<Query, Enumerator>(this, entity => entity.Has<T>() && predicate(entity.Get<T>()));
        }

        public WhereQuery<Query, Enumerator> WhereObject<T>(Func<T, bool> predicate) where T : class
        {
            return new WhereQuery<Query, Enumerator>(this, entity => entity.Has<T>() && predicate(entity.Get<T>()));
        }

        public QueryEntity Single()
        {
            var enumerator = GetEnumerator();
            if (!enumerator.MoveNext()) throw new NoElementsException();
            var entity = enumerator.Current;
            if (enumerator.MoveNext()) throw new MoreThanOneElementsException();
            return entity;
        }

        public QueryEntity SingleOrDefault()
        {
            var enumerator = GetEnumerator();
            if (!enumerator.MoveNext()) return new QueryEntity();
            var entity = enumerator.Current;
            if (enumerator.MoveNext()) throw new MoreThanOneElementsException();
            return entity;
        }

        public QueryEntity First()
        {
            var enumerator = GetEnumerator();
            if (!enumerator.MoveNext()) throw new NoElementsException();
            return enumerator.Current;
        }

        public struct Enumerator : IQueryEnumerator
        {
            private readonly Query _query;
            private int _tableIndex;
            private int _entityIndex;

            public Enumerator(in Query query)
            {
                _query = query;
                _tableIndex = 0;
                _entityIndex = -1;
            }

            public bool MoveNext()
            {
                var tables = _query.Tables;
                if (_tableIndex == tables.Count) return false;

                if (++_entityIndex < tables[_tableIndex].Count) return true;

                _entityIndex = 0;
                _tableIndex++;

                while (_tableIndex < tables.Count && tables[_tableIndex].IsEmpty)
                {
                    _tableIndex++;
                }

                return _tableIndex < tables.Count && _entityIndex < tables[_tableIndex].Count;
            }

            public QueryEntity Current
            {
                get
                {
                    var table = _query.Tables[_tableIndex];
                    var row = table.Rows[_entityIndex];
                    var entity = table.GetStorage<Entity>()[row];
                    return new QueryEntity { Entity = entity, Query = _query };
                }
            }
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

        public static WhereQuery<Query, Query.Enumerator> Where<T>(this in Query query, Func<T, bool> predicate) where T : class
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
