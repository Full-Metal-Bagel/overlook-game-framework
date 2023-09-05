using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RelEcs
{
    public struct StorageType : IComparable<StorageType>
    {
        public Type Type { get; private set; }
        public ulong Value { get; private set; }
        public bool IsRelation { get; private set; }

        public ushort TypeId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => TypeIdConverter.Type(Value);
        }

        public Identity Identity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => TypeIdConverter.Identity(Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StorageType Create(Type type, Identity identity = default)
        {
            return new StorageType()
            {
                Value = TypeIdConverter.Value(type, identity),
                Type = type,
                IsRelation = identity.Id > 0,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StorageType Create<T>(Identity identity = default)
        {
            return new StorageType()
            {
                Value = TypeIdConverter.Value<T>(identity),
                Type = typeof(T),
                IsRelation = identity.Id > 0,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(StorageType other)
        {
            return Value.CompareTo(other.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return (obj is StorageType other) && Value == other.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(StorageType other)
        {
            return Value == other.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return IsRelation ? $"{GetHashCode()} {Type.Name}::{Identity}" : $"{GetHashCode()} {Type.Name}";
        }

        public static bool operator ==(StorageType left, StorageType right) => left.Equals(right);
        public static bool operator !=(StorageType left, StorageType right) => !left.Equals(right);
    }

    public static class TypeIdConverter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Value<T>(Identity identity)
        {
            return (uint)TypeIdAssigner<T>.Id | (ulong)identity.Generation << 16 | (ulong)identity.Id << 32;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Value(Type type, Identity identity)
        {
            return (uint)TypeIdAssigner.GetOrCreate(type).Id | (ulong)identity.Generation << 16 | (ulong)identity.Id << 32;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Identity Identity(ulong value)
        {
            return new Identity((int)(value >> 32), (ushort)(value >> 16));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Type(ulong value)
        {
            return (ushort)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTag<T>()
        {
            return TypeIdAssigner<T>.IsTag;
        }

        private static class TypeIdAssigner
        {
            private static int s_counter;
            private static readonly Dictionary<Type, (int Id, bool IsTag)> s_typeIdMap = new();

            public static (int Id, bool IsTag) GetOrCreate(Type type)
            {
                if (!s_typeIdMap.TryGetValue(type, out var t))
                {
                    var id = Interlocked.Increment(ref s_counter);
                    if (id > ushort.MaxValue) throw new IndexOutOfRangeException();
                    t = (id, IsTagType(type));
                    s_typeIdMap.Add(type, t);
                }
                return t;
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private static class TypeIdAssigner<T>
        {
            // ReSharper disable once StaticMemberInGenericType
            public static readonly int Id;

            // ReSharper disable once StaticMemberInGenericType
            public static readonly bool IsTag;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static TypeIdAssigner()
            {
                (Id, IsTag) = TypeIdAssigner.GetOrCreate(typeof(T));
            }
        }

        private static bool IsTagType(Type type)
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Length == 0;
        }
    }
}
