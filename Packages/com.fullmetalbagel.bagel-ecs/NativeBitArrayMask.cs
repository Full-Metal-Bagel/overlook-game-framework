using System;
using System.Diagnostics.CodeAnalysis;
using Game;
using Unity.Collections;

namespace RelEcs
{
    [DisallowDefaultConstructor]
    [SuppressMessage("Style", "IDE0044:Add readonly modifier")]
    public struct NativeBitArrayMask : IDisposable, IEquatable<NativeBitArrayMask>
    {
        private NativeBitArraySet _hasTypes;
        private NativeBitArraySet _notTypes;
        private NativeBitArraySet _anyTypes;
        private bool _hasAny;

        public StorageType FirstType { get; private set; }

        public static NativeBitArrayMask Create(Allocator allocator = Allocator.Persistent)
        {
            return new NativeBitArrayMask(TypeIdAssigner.MaxTypeCapacity, allocator);
        }

        private NativeBitArrayMask(int _, Allocator allocator)
        {
            _hasAny = false;
            FirstType = StorageType.Create<Entity>();
            _hasTypes = NativeBitArraySet.Create(allocator);
            _notTypes = NativeBitArraySet.Create(allocator);
            _anyTypes = NativeBitArraySet.Create(allocator);
        }

        public void Dispose()
        {
            FirstType = StorageType.Create<Entity>();
            _hasAny = false;
            _hasTypes.Dispose();
            _notTypes.Dispose();
            _anyTypes.Dispose();
        }

        public void Has(StorageType type)
        {
            FirstType = type;
            _hasTypes.Add(type);
        }

        public void Not(StorageType type)
        {
            _notTypes.Add(type);
        }

        public void Any(StorageType type)
        {
            _hasAny = true;
            _anyTypes.Add(type);
        }

        public bool HasTypesContainsAny(Func<StorageType, bool> predicate)
        {
            foreach (var type in _hasTypes)
            {
                if (predicate(type))
                {
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            FirstType = StorageType.Create<Entity>();
            _hasAny = false;
            _hasTypes.Clear();
            _notTypes.Clear();
            _anyTypes.Clear();
        }

        internal bool IsMaskCompatibleWith(NativeBitArraySet set)
        {
            var matchesComponents = set.IsSupersetOf(_hasTypes);
            matchesComponents = matchesComponents && !set.Overlaps(_notTypes);
            matchesComponents = matchesComponents && (!_hasAny || set.Overlaps(_anyTypes));
            return matchesComponents;
        }

        public bool Equals(NativeBitArrayMask other)
        {
            return _hasAny == other._hasAny &&
                   FirstType == other.FirstType &&
                   _hasTypes.Equals(other._hasTypes) &&
                   _notTypes.Equals(other._notTypes) &&
                   _anyTypes.Equals(other._anyTypes);
        }

        public override bool Equals(object? obj) => throw new NotSupportedException();

        public override int GetHashCode() => HashCode.Combine(_hasTypes, _notTypes, _anyTypes, _hasAny, FirstType);
    }
}
