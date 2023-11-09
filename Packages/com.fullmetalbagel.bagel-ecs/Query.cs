using System;
using System.Collections.Generic;
using System.Linq;
using Game;

namespace RelEcs
{
    public readonly struct Query
    {
        internal List<Table> Tables { get; init; }
        internal Archetypes Archetypes { get; init; }
        internal Mask Mask { get; init; }

        internal static readonly Func<Archetypes, Mask, List<Table>, Query> s_createQuery =
            (archetypes, mask, matchingTables) => new Query { Archetypes = archetypes, Mask = mask, Tables = matchingTables };

        internal void AddTable(Table table)
        {
            Tables.Add(table);
        }

        public bool Has(Entity entity)
        {
            var meta = Archetypes.GetEntityMeta(entity.Identity);
            var table = Archetypes.GetTable(meta.TableId);
            return Tables.Contains(table);
        }

        public ref T Get<T>(Entity entity) where T : struct
        {
            Debug.Assert(Mask._hasTypes.Contains(StorageType.Create<T>()));
            var (meta, table) = Get(entity);
            return ref table.GetStorage<T>()[meta.Row];
        }

        public T GetObject<T>(Entity entity) where T : class
        {
            Debug.Assert(Mask._hasTypes.Contains(StorageType.Create<T>()) || Mask._hasTypes.Any(type => typeof(T).IsAssignableFrom(type.Type)));
            var (meta, table) = Get(entity);
            return (T)table.GetStorage(StorageType.Create<T>()).GetValue(meta.Row);
        }

        private (EntityMeta meta, Table table) Get(Entity entity)
        {
            var meta = Archetypes.GetEntityMeta(entity.Identity);
            var table = Archetypes.GetTable(meta.TableId);
            return (meta, table);
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
                    var entity = _query.Tables[_tableIndex].GetStorage<Entity>()[_entityIndex];
                    return new QueryEntity { Entity = entity, Query = _query };
                }
            }
        }

        public readonly ref struct QueryEntity
        {
            public Entity Entity { get; init; }
            public Query Query { get; init; }

            public bool Has()
            {
                return Query.Has(Entity);
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

        public readonly ref struct Builder
        {
            internal Archetypes Archetypes { get; }
            internal Mask Mask { get; }

            public Builder(Archetypes archetypes)
            {
                Archetypes = archetypes;
                Mask = MaskPool.Get();
            }

            public Builder Has<T>()
            {
                var typeIndex = StorageType.Create<T>();
                Mask.Has(typeIndex);
                return this;
            }

            public Builder Not<T>()
            {
                var typeIndex = StorageType.Create<T>();
                Mask.Not(typeIndex);
                return this;
            }

            public Builder Any<T>()
            {
                var typeIndex = StorageType.Create<T>();
                Mask.Any(typeIndex);
                return this;
            }

            public Query Build()
            {
                return Archetypes.GetQuery(Mask, Query.s_createQuery);
            }
        }
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
