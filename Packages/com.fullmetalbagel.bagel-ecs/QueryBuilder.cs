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
        private readonly Archetypes _archetypes;
        [SuppressMessage("Style", "IDE0044:Add readonly modifier")]
        private TMask _mask;

        public QueryBuilder(Archetypes archetypes)
        {
            _archetypes = archetypes;
            _mask = TMask.Create();
        }

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

        public Query Build()
        {
            return _archetypes.GetQuery(_mask, Query.s_createQuery);
        }
    }
}
