using System;
using System.Collections;
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

        internal static StorageType Create(ushort typeId, Allocator _ = Allocator.Persistent)
        {
            Debug.Assert(typeId < TypeIdAssigner.Count);
            var isTag = TypeIdAssigner.IsTag(typeId);
            return new StorageType(typeId, isTag);
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
        private static readonly BitArray s_isTagCache = new(MaxTypeCapacity);

        public static Type GetType(int typeId) => s_types[typeId];
        public static bool IsTag(int typeId) => s_isTagCache[typeId];
        public static int Count => s_counter + 1;

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
                s_isTagCache.Set(typeId, IsTagType(type));
            }
            return typeId;
        }

        private static bool IsTagType(Type type)
        {
            if (!type.IsValueType) return false;
#if UNITY_5_3_OR_NEWER
            var size = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf(type);
            if (size == 0) return true;
#else
            var size = System.Runtime.InteropServices.Marshal.SizeOf(type);
#endif
            if (size > 1) return false;
            // Check the current type for fields
            return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length <= 0;
        }
    }

    internal static class TypeIdAssigner<T>
    {
        public static ushort Id => StorageType.TypeId;
        public static StorageType StorageType { get; }

        static TypeIdAssigner()
        {
            var id = TypeIdAssigner.GetOrCreate(typeof(T));
            StorageType = StorageType.Create(id);
        }
    }
}
