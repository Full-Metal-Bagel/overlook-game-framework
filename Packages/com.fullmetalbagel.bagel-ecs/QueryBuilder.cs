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
        public QueryBuilder Has<T>()
        {
            var typeIndex = StorageType.Create<T>();
            Mask.Has(typeIndex);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueryBuilder Not<T>()
        {
            var typeIndex = StorageType.Create<T>();
            Mask.Not(typeIndex);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueryBuilder Any<T>()
        {
            var typeIndex = StorageType.Create<T>();
            Mask.Any(typeIndex);
            return this;
        }
    }
}
