using System;
using System.Collections.Generic;
using System.Threading;
using Game;

namespace RelEcs
{
    [DisallowDefaultConstructor]
    public readonly struct StorageType : IComparable<StorageType>, IEquatable<StorageType>
    {
        public ushort Value { get; init; }
        public ushort TypeId => Value;
        public Type Type => TypeIdAssigner.GetType(Value);
        public bool IsValueType => Type.IsValueType;
        public static implicit operator ushort(StorageType type) => type.TypeId;

        public static StorageType Create(ushort typeId)
        {
            return new StorageType(typeId);
        }

        public static StorageType Create(Type type)
        {
            return new StorageType(TypeIdAssigner.GetOrCreate(type));
        }

        public static StorageType Create<T>()
        {
            return new StorageType(TypeIdAssigner<T>.Id);
        }

        private StorageType(ushort value)
        {
            Value = value;
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
        public const int MaxTypeCapacity = 512;
        private static int s_counter = -1;
        private static readonly Dictionary<Type, ushort> s_typeIdMap = new();
        private static readonly Type[] s_types = new Type[MaxTypeCapacity];

        public static Type GetType(int typeId) => s_types[typeId];

        public static ushort GetOrCreate(Type type)
        {
            Debug.Assert(!type.IsGenericTypeDefinition);
            Debug.Assert(!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Nullable<>));
            if (!s_typeIdMap.TryGetValue(type, out ushort typeId))
            {
                var id = Interlocked.Increment(ref s_counter);
                if (id is >= MaxTypeCapacity or < 0) throw new IndexOutOfRangeException();
                typeId = (ushort)id;
                s_typeIdMap.Add(type, typeId);
                s_types[typeId] = type;
            }
            return typeId;
        }
    }

    internal static class TypeIdAssigner<T>
    {
        public static readonly ushort Id = TypeIdAssigner.GetOrCreate(typeof(T));
    }
}
