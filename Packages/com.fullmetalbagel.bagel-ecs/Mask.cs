using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RelEcs
{
    public sealed class Mask : IDisposable, IEquatable<Mask>
    {
        public SortedSetTypeSet HasTypes { get; } = SortedSetTypeSet.Create();
        public SortedSetTypeSet NotTypes { get; } = SortedSetTypeSet.Create();
        public SortedSetTypeSet AnyTypes { get; } = SortedSetTypeSet.Create();
        private bool _disposed = false;

        private static readonly Stack<Mask> s_pool = new(32);

        private Mask() { }

        public static Mask Create()
        {
            return s_pool.Count > 0 ? s_pool.Pop() : new Mask();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Clear();
                s_pool.Push(this);
            }
        }

        public StorageType FirstType => HasTypes.Count == 0 ? StorageType.Create<Entity>() : HasTypes.First();

        public bool HasTypesContainsAny(Func<StorageType, bool> predicate)
        {
            return HasTypes.Any(predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Has(StorageType type)
        {
            HasTypes.Add(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Not(StorageType type)
        {
            NotTypes.Add(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Any(StorageType type)
        {
            AnyTypes.Add(type);
        }

        public void Clear()
        {
            HasTypes.Clear();
            NotTypes.Clear();
            AnyTypes.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return HashCode.Combine(HasTypes, NotTypes, AnyTypes);
        }

        internal bool IsMaskCompatibleWith(SortedSetTypeSet set)
        {
            var matchesComponents = set.IsSupersetOf(HasTypes);
            matchesComponents = matchesComponents && !set.Overlaps(NotTypes);
            matchesComponents = matchesComponents && (AnyTypes.Count == 0 || set.Overlaps(AnyTypes));
            return matchesComponents;
        }

        public bool Equals(Mask? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return HasTypes.Equals(other.HasTypes) && NotTypes.Equals(other.NotTypes) && AnyTypes.Equals(other.AnyTypes);
        }

        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Mask other && Equals(other);
    }
}
