using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using Unity.Collections;

namespace Overlook.Ecs;

[SuppressMessage("Design", "CA1036:Override methods on comparable types")]
[DisallowDefaultConstructor]
public readonly record struct StorageType(ushort Value) : IComparable<StorageType>
{
    public ushort TypeId => Value;
    public Type Type => TypeIdAssigner.GetType(Value);
    public bool IsValueType => Type.IsValueType;
    public bool IsTag => TypeIdAssigner.IsTag(Value);
    public int UnmanagedTypeSize => TypeIdAssigner.GetSize(Value);
    public bool IsUnmanagedType => TypeIdAssigner.IsUnmanagedType(Value);
    public static implicit operator ushort(StorageType type) => type.Value;

    internal static StorageType Create(ushort typeId, Allocator _ = Allocator.Persistent)
    {
        Debug.Assert(typeId < TypeIdAssigner.Count);
        return new StorageType(typeId);
    }

    public static StorageType Create(Type type, Allocator _ = Allocator.Persistent)
        => Create(TypeIdAssigner.GetOrCreate(type));
    public static StorageType Create<T>(Allocator _ = Allocator.Persistent) => TypeIdAssigner<T>.StorageType;
    public override string ToString() => $"{Type}(value={Value} tag={IsTag})";

    public void Deconstruct(out Type type, out ushort typeId)
    {
        type = Type;
        typeId = Value;
    }

    public int CompareTo(StorageType other) => Value.CompareTo(other.Value);
}

internal static class TypeIdAssigner
{
    public const int MaxTypeCapacity = 1024;
    private static int s_counter = -1;
    private static readonly Dictionary<Type, ushort> s_typeIdMap = new();
    private static readonly Type[] s_types = new Type[MaxTypeCapacity];
    private static readonly BitArray s_isTagCache = new(MaxTypeCapacity);
    private static readonly int[] s_typeSizes = new int[MaxTypeCapacity];

    public static Type GetType(int typeId)
    {
        Debug.Assert(typeId < Count);
        return s_types[typeId];
    }

    public static bool IsTag(int typeId)
    {
        Debug.Assert(typeId < Count);
        return s_isTagCache[typeId];
    }

    public static int Count => s_counter + 1;

    public static int GetSize(int typeId)
    {
        Debug.Assert(typeId < Count);
        var size = s_typeSizes[typeId];
        Debug.Assert(size >= 0, "invalid type size");
        return size;
    }

    public static bool IsUnmanagedType(int typeId)
    {
        Debug.Assert(typeId < Count);
        return s_typeSizes[typeId] >= 0;
    }

    public static ushort GetOrCreate(Type type)
    {
        Debug.Assert(!type.IsGenericTypeDefinition);
        Debug.Assert(!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Nullable<>));
        if (!s_typeIdMap.TryGetValue(type, out ushort typeId))
        {
            var id = Interlocked.Increment(ref s_counter);
            if (id is >= MaxTypeCapacity or < 0) throw new OutOfTypeIdCapacityException($"please expand the {nameof(MaxTypeCapacity)}");
            typeId = (ushort)id;
            s_typeIdMap.Add(type, typeId);
            s_types[typeId] = type;
            s_isTagCache.Set(typeId, IsTagType(type));
            s_typeSizes[typeId] = type.IsUnmanaged() ? SizeOf(type) : -1;
        }
        return typeId;

        static int SizeOf(Type type)
        {
#if UNITY_5_3_OR_NEWER
            return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf(type);
#else
            return (int)typeof(System.Runtime.CompilerServices.Unsafe).GetMethod("SizeOf")!.MakeGenericMethod(type).Invoke(null, null);
#endif
        }
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
    public static ushort Id => StorageType.Value;
    public static StorageType StorageType { get; }

    static TypeIdAssigner()
    {
        var id = TypeIdAssigner.GetOrCreate(typeof(T));
        StorageType = StorageType.Create(id);
    }
}

public sealed class OutOfTypeIdCapacityException : Exception
{
    public OutOfTypeIdCapacityException() { }
    public OutOfTypeIdCapacityException(string message) : base(message) { }
    public OutOfTypeIdCapacityException(string message, Exception inner) : base(message, inner) { }
}
