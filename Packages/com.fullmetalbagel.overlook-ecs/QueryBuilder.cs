using System;
using System.Diagnostics.CodeAnalysis;
#if OVERLOOK_ECS_USE_UNITY_COLLECTION
using TMask = Overlook.Ecs.NativeBitArrayMask;
#else
using TMask = Overlook.Ecs.Mask;
#endif

namespace Overlook.Ecs;

public ref struct QueryBuilder
{
    private TMask _mask;

    [SuppressMessage("Style", "IDE0044:Add readonly modifier")]
    public TMask Mask
    {
        get => _mask;
        private init => _mask = value;
    }

    public static QueryBuilder Create() => new() { Mask = TMask.Create() };

    public QueryBuilder Has<T>()
    {
        var typeIndex = StorageType.Create<T>();
        _mask.Has(typeIndex);
        return this;
    }

    public QueryBuilder Has(Type type)
    {
        var typeIndex = StorageType.Create(type);
        _mask.Has(typeIndex);
        return this;
    }

    public QueryBuilder Not<T>()
    {
        var typeIndex = StorageType.Create<T>();
        _mask.Not(typeIndex);
        return this;
    }

    public QueryBuilder Not(Type type)
    {
        var typeIndex = StorageType.Create(type);
        _mask.Not(typeIndex);
        return this;
    }

    public QueryBuilder Any(Type type)
    {
        var typeIndex = StorageType.Create(type);
        _mask.Any(typeIndex);
        return this;
    }

    public QueryBuilder Any<T>()
    {
        var typeIndex = StorageType.Create<T>();
        _mask.Any(typeIndex);
        return this;
    }
}

public static class QueryBuilderExtensions
{
    public static Query Build(this QueryBuilder builder, Archetypes archetypes)
    {
        return archetypes.GetQuery(builder.Mask, Query.s_createQuery);
    }

    public static Query Build(this QueryBuilder builder, World world)
    {
        return builder.Build(world.Archetypes);
    }
}