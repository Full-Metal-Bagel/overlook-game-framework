using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace RelEcs
{
    public readonly struct StorageType : IComparable<StorageType>, IEquatable<StorageType>
    {
        public ushort Value { get; init; }
        public ushort TypeId => Value;
        public Type Type => TypeIdAssigner.GetType(Value);
        public static implicit operator ushort(StorageType type) => type.TypeId;

        public static StorageType Create(Type type)
        {
            return new StorageType { Value = TypeIdAssigner.GetOrCreate(type) };
        }

        public static StorageType Create<T>()
        {
            return new StorageType { Value = TypeIdAssigner<T>.Id };
        }

        public void Deconstruct(out Type type, out ushort typeId)
        {
            type = Type;
            typeId = TypeId;
        }

        public bool Equals(StorageType other) => Value == other.Value;
        public override bool Equals(object? obj) => throw new NotSupportedException();
        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(StorageType lhs, StorageType rhs)
        {
            return lhs.Value == rhs.Value;
        }

        public static bool operator !=(StorageType lhs, StorageType rhs)
        {
            return !(lhs == rhs);
        }

        public int CompareTo(StorageType other) => Value.CompareTo(other.Value);
    }

    internal static class TypeIdAssigner
    {
        private static int s_counter;
        private static readonly Dictionary<Type, ushort> s_typeIdMap = new();
        private static readonly List<Type> s_types = new(64);

        public static Type GetType(int typeId) => s_types[typeId - 1];

        public static ushort GetOrCreate(Type type)
        {
            Debug.Assert(!type.IsGenericTypeDefinition);
            Debug.Assert(!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Nullable<>));
            if (!s_typeIdMap.TryGetValue(type, out ushort typeId))
            {
                var id = Interlocked.Increment(ref s_counter);
                if (id > ushort.MaxValue) throw new IndexOutOfRangeException();
                typeId = (ushort)id;
                s_typeIdMap.Add(type, typeId);
                s_types.Add(type);
            }
            return typeId;
        }
    }

    internal static class TypeIdAssigner<T>
    {
        public static readonly ushort Id = TypeIdAssigner.GetOrCreate(typeof(T));
    }
}
