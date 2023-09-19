using System;
using System.Runtime.CompilerServices;

namespace RelEcs
{
    public class QueryBuilder
    {
        internal readonly Archetypes Archetypes;
        protected readonly Mask Mask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueryBuilder(Archetypes archetypes)
        {
            Archetypes = archetypes;
            Mask = MaskPool.Get();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueryBuilder Has<T>(Entity? target = default)
        {
            var typeIndex = StorageType.Create<T>(target?.Identity ?? Identity.None);
            Mask.Has(typeIndex);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueryBuilder Has<T>(Type type)
        {
            var entity = Archetypes.GetTypeEntity(type);
            var typeIndex = StorageType.Create<T>(entity.Identity);
            Mask.Has(typeIndex);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueryBuilder Not<T>(Entity? target = default)
        {
            var typeIndex = StorageType.Create<T>(target?.Identity ?? Identity.None);
            Mask.Not(typeIndex);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueryBuilder Not<T>(Type type)
        {
            var entity = Archetypes.GetTypeEntity(type);
            var typeIndex = StorageType.Create<T>(entity.Identity);
            Mask.Not(typeIndex);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueryBuilder Any<T>(Entity? target = default)
        {
            var typeIndex = StorageType.Create<T>(target?.Identity ?? Identity.None);
            Mask.Any(typeIndex);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueryBuilder Any<T>(Type type)
        {
            var entity = Archetypes.GetTypeEntity(type);
            var typeIndex = StorageType.Create<T>(entity.Identity);
            Mask.Any(typeIndex);
            return this;
        }
    }
}
