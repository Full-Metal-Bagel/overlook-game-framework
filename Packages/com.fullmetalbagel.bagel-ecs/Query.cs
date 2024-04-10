using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
#if ARCHETYPE_USE_NATIVE_BIT_ARRAY
using TMask = RelEcs.NativeBitArrayMask;
#else
using TMask = RelEcs.Mask;
#endif

namespace RelEcs
{
    public readonly struct Query : IEquatable<Query>
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

        public ref struct Enumerator
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

        public readonly ref struct QueryEntity
        {
            public Entity Entity { get; init; }
            public Query Query { get; init; }

            public bool Has<T>()
            {
                return Query.Has<T>(Entity);
            }

            public ref T Get<T>() where T : struct
            {
                return ref Query.Get<T>(Entity);
            }

            public T GetObject<T>() where T : class
            {
                return Query.GetObject<T>(Entity);
            }

            public static implicit operator Entity(QueryEntity self) => self.Entity;
        }

        public ref struct Builder
        {
            private readonly Archetypes _archetypes;
            [SuppressMessage("Style", "IDE0044:Add readonly modifier")]
            private TMask _mask;

            public Builder(Archetypes archetypes)
            {
                _archetypes = archetypes;
                _mask = TMask.Create();
            }

            public Builder Has<T>()
            {
                var typeIndex = StorageType.Create<T>();
                _mask.Has(typeIndex);
                return this;
            }

            public Builder Has(Type type)
            {
                var typeIndex = StorageType.Create(type);
                _mask.Has(typeIndex);
                return this;
            }

            public Builder Not<T>()
            {
                var typeIndex = StorageType.Create<T>();
                _mask.Not(typeIndex);
                return this;
            }

            public Builder Not(Type type)
            {
                var typeIndex = StorageType.Create(type);
                _mask.Not(typeIndex);
                return this;
            }

            public Builder Any(Type type)
            {
                var typeIndex = StorageType.Create(type);
                _mask.Any(typeIndex);
                return this;
            }

            public Builder Any<T>()
            {
                var typeIndex = StorageType.Create<T>();
                _mask.Any(typeIndex);
                return this;
            }

            public Query Build()
            {
                return _archetypes.GetQuery(_mask, s_createQuery);
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

        public static T Get<T>(this in Query.QueryEntity queryEntity) where T : class
        {
            return queryEntity.GetObject<T>();
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
