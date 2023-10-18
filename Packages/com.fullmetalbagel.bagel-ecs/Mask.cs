using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RelEcs
{
    public sealed class Mask
    {
        internal readonly List<StorageType> _hasTypes = new();
        internal readonly List<StorageType> _notTypes = new();
        internal readonly List<StorageType> _anyTypes = new();

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
            var hash = _hasTypes.Count + _anyTypes.Count + _notTypes.Count;

            unchecked
            {
                foreach (var type in _hasTypes) hash = hash * 314159 + type.Value.GetHashCode();
                foreach (var type in _notTypes) hash = hash * 314159 - type.Value.GetHashCode();
                foreach (var type in _anyTypes) hash *= 314159 * type.Value.GetHashCode();
            }

            return hash;
        }
    }
}
