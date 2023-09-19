using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace RelEcs
{
    public readonly struct StorageType : IComparable<StorageType>
    {
        [StructLayout(LayoutKind.Explicit)]
        private readonly struct Data
        {
            [field: FieldOffset(0)] public ulong Value { get; init; }
            [field: FieldOffset(0)] public int Id { get; init; }
            [field: FieldOffset(4)] public ushort Generation { get; init; }
            [field: FieldOffset(6)] public ushort TypeId { get; init; }
        }

        private readonly Data _data;

        public ulong Value => _data.Value;
        public Type Type { get; }
        public bool IsRelation => _data.Id > 0;

        public ushort TypeId => _data.TypeId;
        public Identity Identity => new(id: _data.Id, generation: _data.Generation);

        private StorageType(Identity identity, ushort typeId, Type type)
        {
            _data = new Data { Id = identity.Id, Generation = identity.Generation, TypeId = typeId };
            Type = type;
        }

        public static StorageType Create(Type type)
        {
            return Create(type, Identity.None);
        }

        public static StorageType Create<T>()
        {
            return Create<T>(Identity.None);
        }

        public static StorageType Create(Type type, Identity relationTarget)
        {
            return new StorageType(relationTarget, TypeIdAssigner.GetOrCreate(type).Id, type);
        }

        public static StorageType Create<T>(Identity relationTarget)
        {
            return new StorageType(relationTarget, TypeIdAssigner<T>.Id, typeof(T));
        }

        public int CompareTo(StorageType other)
        {
            return Value.CompareTo(other.Value);
        }

        public override bool Equals(object obj)
        {
            return (obj is StorageType other) && Value == other.Value;
        }

        public bool Equals(StorageType other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return IsRelation ? $"{GetHashCode()} {Type.Name}::{Identity}" : $"{GetHashCode()} {Type.Name}";
        }

        public static bool operator ==(StorageType left, StorageType right) => left.Equals(right);
        public static bool operator !=(StorageType left, StorageType right) => !left.Equals(right);
    }

    internal static class TypeIdAssigner
    {
        private static int s_counter;
        private static readonly Dictionary<Type, (int Id, bool IsTag)> s_typeIdMap = new();

        public static (ushort Id, bool IsTag) GetOrCreate(Type type)
        {
            if (!s_typeIdMap.TryGetValue(type, out var t))
            {
                var id = Interlocked.Increment(ref s_counter);
                if (id > ushort.MaxValue) throw new IndexOutOfRangeException();
                t = (id, IsTagType(type));
                s_typeIdMap.Add(type, t);
            }
            return ((ushort)t.Id, t.IsTag);
        }

        private static bool IsTagType(Type type)
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Length == 0;
        }
    }

    internal static class TypeIdAssigner<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly ushort Id;

        // ReSharper disable once StaticMemberInGenericType
        public static readonly bool IsTag;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static TypeIdAssigner()
        {
            (Id, IsTag) = TypeIdAssigner.GetOrCreate(typeof(T));
        }
    }
}
