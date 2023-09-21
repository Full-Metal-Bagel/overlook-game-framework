using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RelEcs
{
    public class Query
    {
        public readonly List<Table> Tables;

        internal readonly Archetypes Archetypes;
        internal readonly Mask Mask;

        protected readonly Dictionary<int, Array[]> Storages = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Query(Archetypes archetypes, Mask mask, List<Table> tables)
        {
            Tables = tables;
            Archetypes = archetypes;
            Mask = mask;

            UpdateStorages();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(Entity entity)
        {
            var meta = Archetypes.GetEntityMeta(entity.Identity);
            return Storages.ContainsKey(meta.TableId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddTable(Table table)
        {
            Tables.Add(table);
            UpdateStorages();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual Array[] GetStorages(Table table)
        {
            throw new Exception("Invalid Enumerator");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void UpdateStorages()
        {
            Storages.Clear();

            foreach (var table in Tables)
            {
                var storages = GetStorages(table);
                Storages.Add(table.Id, storages);
            }
        }
    }

    public class Query<C0> : Query where C0 : class
    {
        public Query(Archetypes archetypes, Mask mask, List<Table> tables) : base(archetypes, mask, tables)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Array[] GetStorages(Table table)
        {
            return new Array[]
            {
                table.GetStorage<C0>(),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public C0 Get(Entity entity)
        {
            var meta = Archetypes.GetEntityMeta(entity.Identity);
            var storages = Storages[meta.TableId];
            return ((C0[])storages[0])[meta.Row];
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(Tables);
        }

        public ref struct Enumerator
        {
            private QueryEnumerator _data;
            public Enumerator(IReadOnlyList<Table> tables) => _data = new QueryEnumerator(tables);
            public bool MoveNext() => _data.MoveNext();
            public C0 Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _data.Tables[_data.TableIndex].GetStorage<C0>()[_data.EntityIndex];
            }
        }

        public sealed class Builder : QueryBuilder
        {
            static readonly Func<Archetypes, Mask, List<Table>, Query> CreateQuery =
                (archetypes, mask, matchingTables) => new Query<C0>(archetypes, mask, matchingTables);

            public Builder(Archetypes archetypes) : base(archetypes)
            {
                Has<C0>();
            }

            public new Builder Has<T>()
            {
                return (Builder)base.Has<T>();
            }

            public new Builder Not<T>()
            {
                return (Builder)base.Not<T>();
            }

            public new Builder Any<T>()
            {
                return (Builder)base.Any<T>();
            }

            public Query<C0> Build()
            {
                return (Query<C0>)Archetypes.GetQuery(Mask, CreateQuery);
            }
        }
    }
}
