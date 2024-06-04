using System;
using System.Diagnostics.CodeAnalysis;
#if ARCHETYPE_USE_NATIVE_BIT_ARRAY
using TMask = RelEcs.NativeBitArrayMask;
#else
using TMask = RelEcs.Mask;
#endif

namespace RelEcs
{
    public ref struct QueryBuilder
    {
        private TMask _mask;

        [SuppressMessage("Style", "IDE0044:Add readonly modifier")]
        public TMask Mask
        {
            get => _mask;
            private init => _mask = value;
        }

        public static QueryBuilder Create() => new() { Mask = TMask.Create() };

        public QueryBuilder Has<T>()
        {
            var typeIndex = StorageType.Create<T>();
            _mask.Has(typeIndex);
            return this;
        }

        public QueryBuilder Has(Type type)
        {
            var typeIndex = StorageType.Create(type);
            _mask.Has(typeIndex);
            return this;
        }

        public QueryBuilder Not<T>()
        {
            var typeIndex = StorageType.Create<T>();
            _mask.Not(typeIndex);
            return this;
        }

        public QueryBuilder Not(Type type)
        {
            var typeIndex = StorageType.Create(type);
            _mask.Not(typeIndex);
            return this;
        }

        public QueryBuilder Any(Type type)
        {
            var typeIndex = StorageType.Create(type);
            _mask.Any(typeIndex);
            return this;
        }

        public QueryBuilder Any<T>()
        {
            var typeIndex = StorageType.Create<T>();
            _mask.Any(typeIndex);
            return this;
        }
    }

    public static class QueryBuilderExtensions
    {
        public static Query Build(this QueryBuilder builder, Archetypes archetypes)
        {
            return archetypes.GetQuery(builder.Mask, Query.s_createQuery);
        }

        public static Query Build(this QueryBuilder builder, World world)
        {
            return builder.Build(world.Archetypes);
        }
    }
}
