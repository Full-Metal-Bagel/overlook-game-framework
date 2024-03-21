using System;
using System.Collections.Generic;
using System.Linq;

namespace RelEcs
{
    public class SortedSetTypeSet : IDisposable, IEquatable<SortedSetTypeSet>
    {
        private readonly SortedSet<StorageType> _types = new();
        public int Count => _types.Count;
        public StorageType First() => _types.First();

        private SortedSetTypeSet() { }

        public static SortedSetTypeSet Create()
        {
            return new SortedSetTypeSet();
        }

        public static SortedSetTypeSet Create(StorageType type)
        {
            var result = new SortedSetTypeSet();
            result.Add(type);
            return result;
        }

        public static SortedSetTypeSet Create(SortedSetTypeSet set)
        {
            return Create(set._types);
        }

        public static SortedSetTypeSet Create<TEnumerable>(TEnumerable set) where TEnumerable : IEnumerable<StorageType>
        {
            var result = new SortedSetTypeSet();
            foreach (var type in set) result.Add(type);
            return result;
        }

        public bool Any(Func<StorageType, bool> predicate)
        {
            return _types.Any(predicate);
        }

        public void Add(StorageType type)
        {
            _types.Add(type);
        }

        public void Remove(StorageType type)
        {
            _types.Remove(type);
        }

        public bool Contains(StorageType type)
        {
            return _types.Contains(type);
        }

        public void Clear()
        {
            _types.Clear();
        }

        public bool IsSupersetOf(SortedSetTypeSet set)
        {
            return _types.IsSupersetOf(set._types);
        }

        public bool Overlaps(SortedSetTypeSet set)
        {
            return _types.Overlaps(set._types);
        }

        public SortedSet<StorageType>.Enumerator GetEnumerator()
        {
            return _types.GetEnumerator();
        }

        public void Dispose()
        {
            _types.Clear();
        }

        public bool Equals(SortedSetTypeSet? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (_types.Count != other._types.Count)
            {
                return false;
            }

            // Use enumerators to iterate through both sets for comparison
            using var selfEnumerator = _types.GetEnumerator();
            using var otherEnumerator = other._types.GetEnumerator();

            while (selfEnumerator.MoveNext() && otherEnumerator.MoveNext())
            {
                if (selfEnumerator.Current != otherEnumerator.Current)
                {
                    return false; // Found elements that do not match
                }
            }

            return true;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((SortedSetTypeSet)obj);
        }

        public override int GetHashCode()
        {
            var hashcode = 17;
            foreach (var type in _types) hashcode = hashcode * 31 + type.GetHashCode();
            return hashcode;
        }
    }
}
