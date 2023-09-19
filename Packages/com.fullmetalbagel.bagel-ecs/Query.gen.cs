
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RelEcs
{

    public class Query<C0, C1> : Query
        where C0 : class
        where C1 : class
    {
        public Query(Archetypes archetypes, Mask mask, List<Table> tables) : base(archetypes, mask, tables)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Array[] GetStorages(Table table)
        {
            return new Array[]
            {
                table.GetStorage<C0>(Identity.None),
                table.GetStorage<C1>(Identity.None),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (C0, C1) Get(Entity entity)
        {
            var meta = Archetypes.GetEntityMeta(entity.Identity);
            var storages = Storages[meta.TableId];
            return (((C0[])storages[0])[meta.Row], ((C1[])storages[1])[meta.Row]);
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
            public (C0, C1) Current => (_data.Tables[_data.TableIndex].GetStorage<C0>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C1>(Identity.None)[_data.EntityIndex]);
        }

        public sealed class Builder : QueryBuilder
        {
            static readonly Func<Archetypes, Mask, List<Table>, Query> CreateQuery =
                (archetypes, mask, matchingTables) => new Query<C0, C1>(archetypes, mask, matchingTables);

            public Builder(Archetypes archetypes) : base(archetypes)
            {
                Has<C0>().Has<C1>();
            }

            public new Builder Has<T>(Entity? target = default)
            {
                return (Builder)base.Has<T>(target);
            }

            public new Builder Has<T>(Type type)
            {
                return (Builder)base.Has<T>(type);
            }

            public new Builder Not<T>(Entity? target = default)
            {
                return (Builder)base.Not<T>(target);
            }

            public new Builder Not<T>(Type type)
            {
                return (Builder)base.Not<T>(type);
            }

            public new Builder Any<T>(Entity? target = default)
            {
                return (Builder)base.Any<T>(target);
            }

            public new Builder Any<T>(Type type)
            {
                return (Builder)base.Any<T>(type);
            }

            public Query<C0, C1> Build()
            {
                return (Query<C0, C1>)Archetypes.GetQuery(Mask, CreateQuery);
            }
        }
    }

    public static partial class WorldQueryExtension
    {
        public static Query<C0, C1>.Builder Query<C0, C1>(this World world)
        where C0 : class
        where C1 : class
        {
            return new Query<C0, C1>.Builder(world._archetypes);
        }
    }

    public class Query<C0, C1, C2> : Query
        where C0 : class
        where C1 : class
        where C2 : class
    {
        public Query(Archetypes archetypes, Mask mask, List<Table> tables) : base(archetypes, mask, tables)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Array[] GetStorages(Table table)
        {
            return new Array[]
            {
                table.GetStorage<C0>(Identity.None),
                table.GetStorage<C1>(Identity.None),
                table.GetStorage<C2>(Identity.None),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (C0, C1, C2) Get(Entity entity)
        {
            var meta = Archetypes.GetEntityMeta(entity.Identity);
            var storages = Storages[meta.TableId];
            return (((C0[])storages[0])[meta.Row], ((C1[])storages[1])[meta.Row], ((C2[])storages[2])[meta.Row]);
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
            public (C0, C1, C2) Current => (_data.Tables[_data.TableIndex].GetStorage<C0>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C1>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C2>(Identity.None)[_data.EntityIndex]);
        }

        public sealed class Builder : QueryBuilder
        {
            static readonly Func<Archetypes, Mask, List<Table>, Query> CreateQuery =
                (archetypes, mask, matchingTables) => new Query<C0, C1, C2>(archetypes, mask, matchingTables);

            public Builder(Archetypes archetypes) : base(archetypes)
            {
                Has<C0>().Has<C1>().Has<C2>();
            }

            public new Builder Has<T>(Entity? target = default)
            {
                return (Builder)base.Has<T>(target);
            }

            public new Builder Has<T>(Type type)
            {
                return (Builder)base.Has<T>(type);
            }

            public new Builder Not<T>(Entity? target = default)
            {
                return (Builder)base.Not<T>(target);
            }

            public new Builder Not<T>(Type type)
            {
                return (Builder)base.Not<T>(type);
            }

            public new Builder Any<T>(Entity? target = default)
            {
                return (Builder)base.Any<T>(target);
            }

            public new Builder Any<T>(Type type)
            {
                return (Builder)base.Any<T>(type);
            }

            public Query<C0, C1, C2> Build()
            {
                return (Query<C0, C1, C2>)Archetypes.GetQuery(Mask, CreateQuery);
            }
        }
    }

    public static partial class WorldQueryExtension
    {
        public static Query<C0, C1, C2>.Builder Query<C0, C1, C2>(this World world)
        where C0 : class
        where C1 : class
        where C2 : class
        {
            return new Query<C0, C1, C2>.Builder(world._archetypes);
        }
    }

    public class Query<C0, C1, C2, C3> : Query
        where C0 : class
        where C1 : class
        where C2 : class
        where C3 : class
    {
        public Query(Archetypes archetypes, Mask mask, List<Table> tables) : base(archetypes, mask, tables)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Array[] GetStorages(Table table)
        {
            return new Array[]
            {
                table.GetStorage<C0>(Identity.None),
                table.GetStorage<C1>(Identity.None),
                table.GetStorage<C2>(Identity.None),
                table.GetStorage<C3>(Identity.None),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (C0, C1, C2, C3) Get(Entity entity)
        {
            var meta = Archetypes.GetEntityMeta(entity.Identity);
            var storages = Storages[meta.TableId];
            return (((C0[])storages[0])[meta.Row], ((C1[])storages[1])[meta.Row], ((C2[])storages[2])[meta.Row], ((C3[])storages[3])[meta.Row]);
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
            public (C0, C1, C2, C3) Current => (_data.Tables[_data.TableIndex].GetStorage<C0>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C1>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C2>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C3>(Identity.None)[_data.EntityIndex]);
        }

        public sealed class Builder : QueryBuilder
        {
            static readonly Func<Archetypes, Mask, List<Table>, Query> CreateQuery =
                (archetypes, mask, matchingTables) => new Query<C0, C1, C2, C3>(archetypes, mask, matchingTables);

            public Builder(Archetypes archetypes) : base(archetypes)
            {
                Has<C0>().Has<C1>().Has<C2>().Has<C3>();
            }

            public new Builder Has<T>(Entity? target = default)
            {
                return (Builder)base.Has<T>(target);
            }

            public new Builder Has<T>(Type type)
            {
                return (Builder)base.Has<T>(type);
            }

            public new Builder Not<T>(Entity? target = default)
            {
                return (Builder)base.Not<T>(target);
            }

            public new Builder Not<T>(Type type)
            {
                return (Builder)base.Not<T>(type);
            }

            public new Builder Any<T>(Entity? target = default)
            {
                return (Builder)base.Any<T>(target);
            }

            public new Builder Any<T>(Type type)
            {
                return (Builder)base.Any<T>(type);
            }

            public Query<C0, C1, C2, C3> Build()
            {
                return (Query<C0, C1, C2, C3>)Archetypes.GetQuery(Mask, CreateQuery);
            }
        }
    }

    public static partial class WorldQueryExtension
    {
        public static Query<C0, C1, C2, C3>.Builder Query<C0, C1, C2, C3>(this World world)
        where C0 : class
        where C1 : class
        where C2 : class
        where C3 : class
        {
            return new Query<C0, C1, C2, C3>.Builder(world._archetypes);
        }
    }

    public class Query<C0, C1, C2, C3, C4> : Query
        where C0 : class
        where C1 : class
        where C2 : class
        where C3 : class
        where C4 : class
    {
        public Query(Archetypes archetypes, Mask mask, List<Table> tables) : base(archetypes, mask, tables)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Array[] GetStorages(Table table)
        {
            return new Array[]
            {
                table.GetStorage<C0>(Identity.None),
                table.GetStorage<C1>(Identity.None),
                table.GetStorage<C2>(Identity.None),
                table.GetStorage<C3>(Identity.None),
                table.GetStorage<C4>(Identity.None),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (C0, C1, C2, C3, C4) Get(Entity entity)
        {
            var meta = Archetypes.GetEntityMeta(entity.Identity);
            var storages = Storages[meta.TableId];
            return (((C0[])storages[0])[meta.Row], ((C1[])storages[1])[meta.Row], ((C2[])storages[2])[meta.Row], ((C3[])storages[3])[meta.Row], ((C4[])storages[4])[meta.Row]);
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
            public (C0, C1, C2, C3, C4) Current => (_data.Tables[_data.TableIndex].GetStorage<C0>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C1>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C2>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C3>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C4>(Identity.None)[_data.EntityIndex]);
        }

        public sealed class Builder : QueryBuilder
        {
            static readonly Func<Archetypes, Mask, List<Table>, Query> CreateQuery =
                (archetypes, mask, matchingTables) => new Query<C0, C1, C2, C3, C4>(archetypes, mask, matchingTables);

            public Builder(Archetypes archetypes) : base(archetypes)
            {
                Has<C0>().Has<C1>().Has<C2>().Has<C3>().Has<C4>();
            }

            public new Builder Has<T>(Entity? target = default)
            {
                return (Builder)base.Has<T>(target);
            }

            public new Builder Has<T>(Type type)
            {
                return (Builder)base.Has<T>(type);
            }

            public new Builder Not<T>(Entity? target = default)
            {
                return (Builder)base.Not<T>(target);
            }

            public new Builder Not<T>(Type type)
            {
                return (Builder)base.Not<T>(type);
            }

            public new Builder Any<T>(Entity? target = default)
            {
                return (Builder)base.Any<T>(target);
            }

            public new Builder Any<T>(Type type)
            {
                return (Builder)base.Any<T>(type);
            }

            public Query<C0, C1, C2, C3, C4> Build()
            {
                return (Query<C0, C1, C2, C3, C4>)Archetypes.GetQuery(Mask, CreateQuery);
            }
        }
    }

    public static partial class WorldQueryExtension
    {
        public static Query<C0, C1, C2, C3, C4>.Builder Query<C0, C1, C2, C3, C4>(this World world)
        where C0 : class
        where C1 : class
        where C2 : class
        where C3 : class
        where C4 : class
        {
            return new Query<C0, C1, C2, C3, C4>.Builder(world._archetypes);
        }
    }

    public class Query<C0, C1, C2, C3, C4, C5> : Query
        where C0 : class
        where C1 : class
        where C2 : class
        where C3 : class
        where C4 : class
        where C5 : class
    {
        public Query(Archetypes archetypes, Mask mask, List<Table> tables) : base(archetypes, mask, tables)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Array[] GetStorages(Table table)
        {
            return new Array[]
            {
                table.GetStorage<C0>(Identity.None),
                table.GetStorage<C1>(Identity.None),
                table.GetStorage<C2>(Identity.None),
                table.GetStorage<C3>(Identity.None),
                table.GetStorage<C4>(Identity.None),
                table.GetStorage<C5>(Identity.None),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (C0, C1, C2, C3, C4, C5) Get(Entity entity)
        {
            var meta = Archetypes.GetEntityMeta(entity.Identity);
            var storages = Storages[meta.TableId];
            return (((C0[])storages[0])[meta.Row], ((C1[])storages[1])[meta.Row], ((C2[])storages[2])[meta.Row], ((C3[])storages[3])[meta.Row], ((C4[])storages[4])[meta.Row], ((C5[])storages[5])[meta.Row]);
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
            public (C0, C1, C2, C3, C4, C5) Current => (_data.Tables[_data.TableIndex].GetStorage<C0>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C1>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C2>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C3>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C4>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C5>(Identity.None)[_data.EntityIndex]);
        }

        public sealed class Builder : QueryBuilder
        {
            static readonly Func<Archetypes, Mask, List<Table>, Query> CreateQuery =
                (archetypes, mask, matchingTables) => new Query<C0, C1, C2, C3, C4, C5>(archetypes, mask, matchingTables);

            public Builder(Archetypes archetypes) : base(archetypes)
            {
                Has<C0>().Has<C1>().Has<C2>().Has<C3>().Has<C4>().Has<C5>();
            }

            public new Builder Has<T>(Entity? target = default)
            {
                return (Builder)base.Has<T>(target);
            }

            public new Builder Has<T>(Type type)
            {
                return (Builder)base.Has<T>(type);
            }

            public new Builder Not<T>(Entity? target = default)
            {
                return (Builder)base.Not<T>(target);
            }

            public new Builder Not<T>(Type type)
            {
                return (Builder)base.Not<T>(type);
            }

            public new Builder Any<T>(Entity? target = default)
            {
                return (Builder)base.Any<T>(target);
            }

            public new Builder Any<T>(Type type)
            {
                return (Builder)base.Any<T>(type);
            }

            public Query<C0, C1, C2, C3, C4, C5> Build()
            {
                return (Query<C0, C1, C2, C3, C4, C5>)Archetypes.GetQuery(Mask, CreateQuery);
            }
        }
    }

    public static partial class WorldQueryExtension
    {
        public static Query<C0, C1, C2, C3, C4, C5>.Builder Query<C0, C1, C2, C3, C4, C5>(this World world)
        where C0 : class
        where C1 : class
        where C2 : class
        where C3 : class
        where C4 : class
        where C5 : class
        {
            return new Query<C0, C1, C2, C3, C4, C5>.Builder(world._archetypes);
        }
    }

    public class Query<C0, C1, C2, C3, C4, C5, C6> : Query
        where C0 : class
        where C1 : class
        where C2 : class
        where C3 : class
        where C4 : class
        where C5 : class
        where C6 : class
    {
        public Query(Archetypes archetypes, Mask mask, List<Table> tables) : base(archetypes, mask, tables)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Array[] GetStorages(Table table)
        {
            return new Array[]
            {
                table.GetStorage<C0>(Identity.None),
                table.GetStorage<C1>(Identity.None),
                table.GetStorage<C2>(Identity.None),
                table.GetStorage<C3>(Identity.None),
                table.GetStorage<C4>(Identity.None),
                table.GetStorage<C5>(Identity.None),
                table.GetStorage<C6>(Identity.None),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (C0, C1, C2, C3, C4, C5, C6) Get(Entity entity)
        {
            var meta = Archetypes.GetEntityMeta(entity.Identity);
            var storages = Storages[meta.TableId];
            return (((C0[])storages[0])[meta.Row], ((C1[])storages[1])[meta.Row], ((C2[])storages[2])[meta.Row], ((C3[])storages[3])[meta.Row], ((C4[])storages[4])[meta.Row], ((C5[])storages[5])[meta.Row], ((C6[])storages[6])[meta.Row]);
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
            public (C0, C1, C2, C3, C4, C5, C6) Current => (_data.Tables[_data.TableIndex].GetStorage<C0>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C1>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C2>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C3>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C4>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C5>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C6>(Identity.None)[_data.EntityIndex]);
        }

        public sealed class Builder : QueryBuilder
        {
            static readonly Func<Archetypes, Mask, List<Table>, Query> CreateQuery =
                (archetypes, mask, matchingTables) => new Query<C0, C1, C2, C3, C4, C5, C6>(archetypes, mask, matchingTables);

            public Builder(Archetypes archetypes) : base(archetypes)
            {
                Has<C0>().Has<C1>().Has<C2>().Has<C3>().Has<C4>().Has<C5>().Has<C6>();
            }

            public new Builder Has<T>(Entity? target = default)
            {
                return (Builder)base.Has<T>(target);
            }

            public new Builder Has<T>(Type type)
            {
                return (Builder)base.Has<T>(type);
            }

            public new Builder Not<T>(Entity? target = default)
            {
                return (Builder)base.Not<T>(target);
            }

            public new Builder Not<T>(Type type)
            {
                return (Builder)base.Not<T>(type);
            }

            public new Builder Any<T>(Entity? target = default)
            {
                return (Builder)base.Any<T>(target);
            }

            public new Builder Any<T>(Type type)
            {
                return (Builder)base.Any<T>(type);
            }

            public Query<C0, C1, C2, C3, C4, C5, C6> Build()
            {
                return (Query<C0, C1, C2, C3, C4, C5, C6>)Archetypes.GetQuery(Mask, CreateQuery);
            }
        }
    }

    public static partial class WorldQueryExtension
    {
        public static Query<C0, C1, C2, C3, C4, C5, C6>.Builder Query<C0, C1, C2, C3, C4, C5, C6>(this World world)
        where C0 : class
        where C1 : class
        where C2 : class
        where C3 : class
        where C4 : class
        where C5 : class
        where C6 : class
        {
            return new Query<C0, C1, C2, C3, C4, C5, C6>.Builder(world._archetypes);
        }
    }

    public class Query<C0, C1, C2, C3, C4, C5, C6, C7> : Query
        where C0 : class
        where C1 : class
        where C2 : class
        where C3 : class
        where C4 : class
        where C5 : class
        where C6 : class
        where C7 : class
    {
        public Query(Archetypes archetypes, Mask mask, List<Table> tables) : base(archetypes, mask, tables)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Array[] GetStorages(Table table)
        {
            return new Array[]
            {
                table.GetStorage<C0>(Identity.None),
                table.GetStorage<C1>(Identity.None),
                table.GetStorage<C2>(Identity.None),
                table.GetStorage<C3>(Identity.None),
                table.GetStorage<C4>(Identity.None),
                table.GetStorage<C5>(Identity.None),
                table.GetStorage<C6>(Identity.None),
                table.GetStorage<C7>(Identity.None),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (C0, C1, C2, C3, C4, C5, C6, C7) Get(Entity entity)
        {
            var meta = Archetypes.GetEntityMeta(entity.Identity);
            var storages = Storages[meta.TableId];
            return (((C0[])storages[0])[meta.Row], ((C1[])storages[1])[meta.Row], ((C2[])storages[2])[meta.Row], ((C3[])storages[3])[meta.Row], ((C4[])storages[4])[meta.Row], ((C5[])storages[5])[meta.Row], ((C6[])storages[6])[meta.Row], ((C7[])storages[7])[meta.Row]);
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
            public (C0, C1, C2, C3, C4, C5, C6, C7) Current => (_data.Tables[_data.TableIndex].GetStorage<C0>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C1>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C2>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C3>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C4>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C5>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C6>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C7>(Identity.None)[_data.EntityIndex]);
        }

        public sealed class Builder : QueryBuilder
        {
            static readonly Func<Archetypes, Mask, List<Table>, Query> CreateQuery =
                (archetypes, mask, matchingTables) => new Query<C0, C1, C2, C3, C4, C5, C6, C7>(archetypes, mask, matchingTables);

            public Builder(Archetypes archetypes) : base(archetypes)
            {
                Has<C0>().Has<C1>().Has<C2>().Has<C3>().Has<C4>().Has<C5>().Has<C6>().Has<C7>();
            }

            public new Builder Has<T>(Entity? target = default)
            {
                return (Builder)base.Has<T>(target);
            }

            public new Builder Has<T>(Type type)
            {
                return (Builder)base.Has<T>(type);
            }

            public new Builder Not<T>(Entity? target = default)
            {
                return (Builder)base.Not<T>(target);
            }

            public new Builder Not<T>(Type type)
            {
                return (Builder)base.Not<T>(type);
            }

            public new Builder Any<T>(Entity? target = default)
            {
                return (Builder)base.Any<T>(target);
            }

            public new Builder Any<T>(Type type)
            {
                return (Builder)base.Any<T>(type);
            }

            public Query<C0, C1, C2, C3, C4, C5, C6, C7> Build()
            {
                return (Query<C0, C1, C2, C3, C4, C5, C6, C7>)Archetypes.GetQuery(Mask, CreateQuery);
            }
        }
    }

    public static partial class WorldQueryExtension
    {
        public static Query<C0, C1, C2, C3, C4, C5, C6, C7>.Builder Query<C0, C1, C2, C3, C4, C5, C6, C7>(this World world)
        where C0 : class
        where C1 : class
        where C2 : class
        where C3 : class
        where C4 : class
        where C5 : class
        where C6 : class
        where C7 : class
        {
            return new Query<C0, C1, C2, C3, C4, C5, C6, C7>.Builder(world._archetypes);
        }
    }

    public class Query<C0, C1, C2, C3, C4, C5, C6, C7, C8> : Query
        where C0 : class
        where C1 : class
        where C2 : class
        where C3 : class
        where C4 : class
        where C5 : class
        where C6 : class
        where C7 : class
        where C8 : class
    {
        public Query(Archetypes archetypes, Mask mask, List<Table> tables) : base(archetypes, mask, tables)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Array[] GetStorages(Table table)
        {
            return new Array[]
            {
                table.GetStorage<C0>(Identity.None),
                table.GetStorage<C1>(Identity.None),
                table.GetStorage<C2>(Identity.None),
                table.GetStorage<C3>(Identity.None),
                table.GetStorage<C4>(Identity.None),
                table.GetStorage<C5>(Identity.None),
                table.GetStorage<C6>(Identity.None),
                table.GetStorage<C7>(Identity.None),
                table.GetStorage<C8>(Identity.None),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (C0, C1, C2, C3, C4, C5, C6, C7, C8) Get(Entity entity)
        {
            var meta = Archetypes.GetEntityMeta(entity.Identity);
            var storages = Storages[meta.TableId];
            return (((C0[])storages[0])[meta.Row], ((C1[])storages[1])[meta.Row], ((C2[])storages[2])[meta.Row], ((C3[])storages[3])[meta.Row], ((C4[])storages[4])[meta.Row], ((C5[])storages[5])[meta.Row], ((C6[])storages[6])[meta.Row], ((C7[])storages[7])[meta.Row], ((C8[])storages[8])[meta.Row]);
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
            public (C0, C1, C2, C3, C4, C5, C6, C7, C8) Current => (_data.Tables[_data.TableIndex].GetStorage<C0>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C1>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C2>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C3>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C4>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C5>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C6>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C7>(Identity.None)[_data.EntityIndex], _data.Tables[_data.TableIndex].GetStorage<C8>(Identity.None)[_data.EntityIndex]);
        }

        public sealed class Builder : QueryBuilder
        {
            static readonly Func<Archetypes, Mask, List<Table>, Query> CreateQuery =
                (archetypes, mask, matchingTables) => new Query<C0, C1, C2, C3, C4, C5, C6, C7, C8>(archetypes, mask, matchingTables);

            public Builder(Archetypes archetypes) : base(archetypes)
            {
                Has<C0>().Has<C1>().Has<C2>().Has<C3>().Has<C4>().Has<C5>().Has<C6>().Has<C7>().Has<C8>();
            }

            public new Builder Has<T>(Entity? target = default)
            {
                return (Builder)base.Has<T>(target);
            }

            public new Builder Has<T>(Type type)
            {
                return (Builder)base.Has<T>(type);
            }

            public new Builder Not<T>(Entity? target = default)
            {
                return (Builder)base.Not<T>(target);
            }

            public new Builder Not<T>(Type type)
            {
                return (Builder)base.Not<T>(type);
            }

            public new Builder Any<T>(Entity? target = default)
            {
                return (Builder)base.Any<T>(target);
            }

            public new Builder Any<T>(Type type)
            {
                return (Builder)base.Any<T>(type);
            }

            public Query<C0, C1, C2, C3, C4, C5, C6, C7, C8> Build()
            {
                return (Query<C0, C1, C2, C3, C4, C5, C6, C7, C8>)Archetypes.GetQuery(Mask, CreateQuery);
            }
        }
    }

    public static partial class WorldQueryExtension
    {
        public static Query<C0, C1, C2, C3, C4, C5, C6, C7, C8>.Builder Query<C0, C1, C2, C3, C4, C5, C6, C7, C8>(this World world)
        where C0 : class
        where C1 : class
        where C2 : class
        where C3 : class
        where C4 : class
        where C5 : class
        where C6 : class
        where C7 : class
        where C8 : class
        {
            return new Query<C0, C1, C2, C3, C4, C5, C6, C7, C8>.Builder(world._archetypes);
        }
    }

}

