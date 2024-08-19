#if ARCHETYPE_USE_NATIVE_BIT_ARRAY

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Game;
using Unity.Collections;
using Unity.Mathematics;

namespace RelEcs
{
    [DisallowDefaultConstructor]
    public struct NativeBitArraySet : IDisposable, IEquatable<NativeBitArraySet>
    {
        [SuppressMessage("Style", "IDE0044:Add readonly modifier")]
        private NativeBitArray _bits;

        public static NativeBitArraySet Create(Allocator allocator = Allocator.Persistent)
        {
            return new NativeBitArraySet(TypeIdAssigner.MaxTypeCapacity, allocator);
        }

        public static NativeBitArraySet Create(StorageType type, Allocator allocator = Allocator.Persistent)
        {
            var result = Create(allocator);
            result.Add(type);
            return result;
        }

        public static NativeBitArraySet Create(NativeBitArraySet set, Allocator allocator = Allocator.Persistent)
        {
            var result = Create(allocator);
            result._bits.Copy(0, ref set._bits, 0, result._bits.Length);
            return result;
        }

        public static NativeBitArraySet Create<TEnumerable>(TEnumerable set, Allocator allocator = Allocator.Persistent) where TEnumerable : IEnumerable<StorageType>
        {
            var result = Create(allocator);
            foreach (var type in set) result.Add(type);
            return result;
        }

        private NativeBitArraySet(int capacity, Allocator allocator)
        {
            _bits = new NativeBitArray(capacity, allocator: allocator);
        }

        public void Add(StorageType type)
        {
            _bits.Set(type.TypeId, true);
        }

        public void Remove(StorageType type)
        {
            _bits.Set(type.TypeId, false);
        }

        public void Clear()
        {
            _bits.Clear();
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_bits);
        }

        public bool Contains(StorageType type)
        {
            return _bits.IsSet(type.TypeId);
        }

        public bool IsSupersetOf(NativeBitArraySet set)
        {
            return _bits.IsSupersetOf(set._bits);
        }

        public bool Overlaps(NativeBitArraySet set)
        {
            return _bits.Overlaps(set._bits);
        }

        public struct Enumerator : IEnumerator<StorageType>
        {
            private readonly NativeBitArray _bits;
            private readonly int _length;
            private int _currentIndex;

            public Enumerator(NativeBitArray bits)
            {
                _bits = bits;
                _currentIndex = -1;
                _length = math.min(_bits.Length, TypeIdAssigner.Count);
            }

            public bool MoveNext()
            {
                _currentIndex++;
                while (_currentIndex < _length)
                {
                    if (_bits.IsSet(_currentIndex)) return true;
                    _currentIndex++;
                }
                return false;
            }

            public void Reset()
            {
                _currentIndex = -1;
            }

            object IEnumerator.Current => Current;

            public StorageType Current
            {
                get
                {
                    Debug.Assert(_currentIndex >= 0 && _currentIndex < math.min(_bits.Length, TypeIdAssigner.Count));
                    return StorageType.Create((ushort)_currentIndex);
                }
            }

            public void Dispose() { }
        }

        public void Dispose() => _bits.Dispose();

        public bool Equals(NativeBitArraySet other)
        {
            return _bits.IsEquals(other._bits);
        }

        public override bool Equals(object? obj)
        {
            return obj is NativeBitArraySet other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _bits.CalculateHashCode();
        }
    }

    public static class NativeBitArrayExtension
    {
        public static bool IsEquals(this NativeBitArray bitArray, NativeBitArray checkBitArray)
        {
            // TODO: check the ptr first?
            if (bitArray.Length != checkBitArray.Length) return false;
            Debug.Assert(bitArray.Length % sizeof(int) == 0);
            Debug.Assert(checkBitArray.Length % sizeof(int) == 0);
            var array = bitArray.AsNativeArray<int>();
            var checkArray = checkBitArray.AsNativeArray<int>();
            for (var i = 0; i < checkArray.Length; i++)
            {
                if (array[i] != checkArray[i]) return false;
            }
            return true;
        }

        public static int CalculateHashCode(this NativeBitArray bitArray)
        {
            Debug.Assert(bitArray.Length % sizeof(int) == 0);
            var hash = 371;
            var array = bitArray.AsNativeArray<int>();
            for (var i = 0; i < array.Length; i++) hash = hash * 31 + array[i];
            return hash;
        }

        public static bool IsSupersetOf(this NativeBitArray bitArray, NativeBitArray checkBitArray)
        {
            Debug.Assert(bitArray.Length % sizeof(int) == 0);
            Debug.Assert(checkBitArray.Length % sizeof(int) == 0);
            Debug.Assert(bitArray.Length >= checkBitArray.Length);
            var array = bitArray.AsNativeArray<int>();
            var checkArray = checkBitArray.AsNativeArray<int>();
            for (var i = 0; i < checkArray.Length; i++)
            {
                var bits = array[i];
                var checkBits = checkArray[i];
                if ((bits & checkBits) != checkBits) return false;
            }
            return true;
        }

        public static bool Overlaps(this NativeBitArray bitArray, NativeBitArray checkBitArray)
        {
            Debug.Assert(bitArray.Length % sizeof(int) == 0);
            Debug.Assert(checkBitArray.Length % sizeof(int) == 0);
            Debug.Assert(bitArray.Length >= checkBitArray.Length);
            var array = bitArray.AsNativeArray<int>();
            var checkArray = checkBitArray.AsNativeArray<int>();
            for (var i = 0; i < checkArray.Length; i++)
            {
                var bits = array[i];
                var checkBits = checkArray[i];
                if ((bits & checkBits) != 0) return true;
            }
            return false;
        }
    }
}

#endif
