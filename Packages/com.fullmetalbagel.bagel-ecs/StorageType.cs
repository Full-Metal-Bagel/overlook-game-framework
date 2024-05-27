using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Game;
using Unity.Collections;
using Debug = Game.Debug;

namespace RelEcs
{
    [DisallowDefaultConstructor]
    public readonly struct StorageType : IComparable<StorageType>, IEquatable<StorageType>
    {
        public bool IsTag { get; }
        public ushort Value { get; }
        public ushort TypeId => Value;
        public Type Type => TypeIdAssigner.GetType(Value);
        public bool IsValueType => Type.IsValueType;
        public static implicit operator ushort(StorageType type) => type.TypeId;

        public static StorageType Create(ushort typeId, Allocator _ = Allocator.Persistent)
        {
            var type = TypeIdAssigner.GetType(typeId);
            return new StorageType(typeId, IsTagType(type));
        }

        public static StorageType Create(Type type, Allocator _ = Allocator.Persistent)
        {
            return Create(TypeIdAssigner.GetOrCreate(type));
        }

        public static StorageType Create<T>(Allocator _ = Allocator.Persistent)
        {
            return TypeIdAssigner<T>.StorageType;
        }

        private StorageType(ushort value, bool isTagType)
        {
            Value = value;
            IsTag = isTagType;
        }

        public override string ToString()
        {
            return $"{Type}(value={IsValueType} tag={IsTag})";
        }

        public static bool IsTagType(Type type)
        {
            // Check the current type and all base types for fields
            while (type != null && type != typeof(object) && type != typeof(ValueType))
            {
                if (type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length > 0)
                {
                    return false; // Fields are found, thus not empty
                }
                type = type.BaseType; // Move to the base class
            }
            return true; // No fields found in any base class or itself
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
        public static ushort Id { get; }
        public static StorageType StorageType { get; }

        static TypeIdAssigner()
        {
            Id = TypeIdAssigner.GetOrCreate(typeof(T));
            StorageType = StorageType.Create(Id);
        }
    }
}
