#if OVERLOOK_ECS_USE_UNITY_COLLECTION

using System;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using Unity.Jobs;

namespace Overlook.Ecs;

[DisallowDefaultConstructor]
[SuppressMessage("Style", "IDE0044:Add readonly modifier")]
public struct NativeBitArrayMask : INativeDisposable, IEquatable<NativeBitArrayMask>
{
    public NativeBitArraySet HasTypes { get; }
    public NativeBitArraySet NotTypes { get; }
    public NativeBitArraySet AnyTypes { get; }
    public bool HasAny { get; private set; }

    public StorageType FirstType { get; private set; }

    public static NativeBitArrayMask Create(Allocator allocator = Allocator.Persistent)
    {
        return new NativeBitArrayMask(TypeIdAssigner.MaxTypeCapacity, allocator);
    }

    private NativeBitArrayMask(int _, Allocator allocator)
    {
        HasAny = false;
        FirstType = StorageType.Create<Entity>();
        HasTypes = NativeBitArraySet.Create(allocator);
        NotTypes = NativeBitArraySet.Create(allocator);
        AnyTypes = NativeBitArraySet.Create(allocator);
    }

    public void Dispose()
    {
        FirstType = StorageType.Create<Entity>();
        HasAny = false;
        HasTypes.Dispose();
        NotTypes.Dispose();
        AnyTypes.Dispose();
    }

    public JobHandle Dispose(JobHandle inputDeps)
    {
        FirstType = StorageType.Create<Entity>();
        HasAny = false;
        return JobHandle.CombineDependencies(
            HasTypes.Dispose(inputDeps),
            NotTypes.Dispose(inputDeps),
            AnyTypes.Dispose(inputDeps)
        );
    }

    public void Has(StorageType type)
    {
        FirstType = type;
        HasTypes.Add(type);
    }

    public void Not(StorageType type)
    {
        NotTypes.Add(type);
    }

    public void Any(StorageType type)
    {
        HasAny = true;
        AnyTypes.Add(type);
    }

    public bool HasTypesContainsAny(Func<StorageType, bool> predicate)
    {
        foreach (var type in HasTypes)
        {
            if (predicate(type))
            {
                return true;
            }
        }
        return false;
    }

    public void Clear()
    {
        FirstType = StorageType.Create<Entity>();
        HasAny = false;
        HasTypes.Clear();
        NotTypes.Clear();
        AnyTypes.Clear();
    }

    internal bool IsMaskCompatibleWith(NativeBitArraySet set)
    {
        var matchesComponents = set.IsSupersetOf(HasTypes);
        matchesComponents = matchesComponents && !set.Overlaps(NotTypes);
        matchesComponents = matchesComponents && (!HasAny || set.Overlaps(AnyTypes));
        return matchesComponents;
    }

    public bool Equals(NativeBitArrayMask other)
    {
        return HasAny == other.HasAny &&
               FirstType == other.FirstType &&
               HasTypes.Equals(other.HasTypes) &&
               NotTypes.Equals(other.NotTypes) &&
               AnyTypes.Equals(other.AnyTypes);
    }

    public override bool Equals(object? obj) => throw new NotSupportedException();

    public override int GetHashCode() => HashCode.Combine(HasTypes, NotTypes, AnyTypes, HasAny, FirstType);
}

#endif
