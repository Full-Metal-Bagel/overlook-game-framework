using System;
using System.Collections.Generic;
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
            return new StorageType(relationTarget, TypeIdAssigner.GetOrCreate(type), type);
        }

        public static StorageType Create<T>(Identity relationTarget)
        {
            return new StorageType(relationTarget, TypeIdAssigner<T>.Id, typeof(T));
        }

        public int CompareTo(StorageType other)
        {
            return Value.CompareTo(other.Value);
        }

        public override bool Equals(object? obj)
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
        private static readonly Dictionary<Type, ushort> s_typeIdMap = new();

        public static ushort GetOrCreate(Type type)
        {
            if (!s_typeIdMap.TryGetValue(type, out var typeId))
            {
                var id = Interlocked.Increment(ref s_counter);
                if (id > ushort.MaxValue) throw new IndexOutOfRangeException();
                typeId = (ushort)id;
                s_typeIdMap.Add(type, typeId);
            }
            return typeId;
        }
    }

    internal static class TypeIdAssigner<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly ushort Id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static TypeIdAssigner()
        {
            Id = TypeIdAssigner.GetOrCreate(typeof(T));
        }
    }
}
