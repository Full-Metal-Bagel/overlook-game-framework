using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RelEcs
{
    public sealed class Mask : IDisposable, IEquatable<Mask>
    {
        internal readonly SortedSetTypeSet _hasTypes = SortedSetTypeSet.Create();
        private readonly SortedSetTypeSet _notTypes = SortedSetTypeSet.Create();
        private readonly SortedSetTypeSet _anyTypes = SortedSetTypeSet.Create();
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

        public StorageType FirstType => _hasTypes.Count == 0 ? StorageType.Create<Entity>() : _hasTypes.First();

        public bool HasTypesContainsAny(Func<StorageType, bool> predicate)
        {
            return _hasTypes.Any(predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Has(StorageType type)
        {
            _hasTypes.Add(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Not(StorageType type)
        {
            _notTypes.Add(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Any(StorageType type)
        {
            _anyTypes.Add(type);
        }

        public void Clear()
        {
            _hasTypes.Clear();
            _notTypes.Clear();
            _anyTypes.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return HashCode.Combine(_hasTypes, _notTypes, _anyTypes);
        }

        internal bool IsMaskCompatibleWith(SortedSetTypeSet set)
        {
            var matchesComponents = set.IsSupersetOf(_hasTypes);
            matchesComponents = matchesComponents && !set.Overlaps(_notTypes);
            matchesComponents = matchesComponents && (_anyTypes.Count == 0 || set.Overlaps(_anyTypes));
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

            return _hasTypes.Equals(other._hasTypes) && _notTypes.Equals(other._notTypes) && _anyTypes.Equals(other._anyTypes);
        }

        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Mask other && Equals(other);
    }
}
