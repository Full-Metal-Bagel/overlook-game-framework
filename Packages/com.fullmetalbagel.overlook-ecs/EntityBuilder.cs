﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Overlook.Pool;

namespace Overlook.Ecs;

public readonly struct EntityBuilder : IComponentsBuilder
{
    public static EntityBuilder Create() => new();
    public static DynamicBuilder CreateDynamic() => new();
    public void CollectTypes<TCollection>(TCollection types) where TCollection : ICollection<StorageType> { }
    public void Build(ArchetypesBuilder archetypes, Identity entityIdentity) { }
    public void Dispose() { }
}

public sealed class DynamicBuilder : IComponentsBuilder
{
    private readonly List<IComponentsBuilder> _builders = new();
    public ICollection<IComponentsBuilder> Builders => _builders;

    public void CollectTypes<TCollection>(TCollection types) where TCollection : ICollection<StorageType>
    {
        foreach (var builder in _builders) builder.CollectTypes(types);
    }

    public void Build(ArchetypesBuilder archetypes, Identity entityIdentity)
    {
        foreach (var builder in _builders) builder.Build(archetypes, entityIdentity);
    }

    public void Dispose()
    {
        foreach (var builder in _builders) builder.Dispose();
    }

    public void AddDynamicRawData(ReadOnlyMemory<byte> data, Type type)
    {
        AddDynamicBuilder(DynamicRawBuilderPool.CreateBuilder(data, type));
    }

    public void AddDynamicBuilder(IComponentsBuilder builder)
    {
        _builders.Add(builder);
    }

    public void AddDynamicData<T>(T data) where T : unmanaged
    {
        AddDynamicBuilder(DynamicBuilderPool<T>.CreateBuilder(data));
    }

    public void AddDynamicObject(object data)
    {
        AddDynamicBuilder(DynamicBuilderPool.CreateBuilder(data));
    }

    static class DynamicBuilderPool<T> where T : struct
    {
        private static readonly IObjectPool<PooledBuilder> s_pool = new ObjectPool<PooledBuilder, PooledBuilderPolicy>();

        private readonly record struct PooledBuilderPolicy : IObjectPoolPolicy
        {
            public object Create() => new PooledBuilder(s_pool);
        }

        public static PooledBuilder CreateBuilder(T value)
        {
            var builder = s_pool.Rent();
            builder.Value = value;
            return builder;
        }

        public sealed class PooledBuilder : IComponentsBuilder
        {
            private IObjectPool<PooledBuilder>? _pool;
            public T Value { get; set; } = default!;

            public PooledBuilder(IObjectPool<PooledBuilder>? pool)
            {
                _pool = pool;
            }

            public void CollectTypes<TCollection>(TCollection types) where TCollection : ICollection<StorageType>
            {
                types.Add(StorageType.Create<T>());
            }

            public void Build(ArchetypesBuilder archetypes, Identity entityIdentity)
            {
                archetypes.SetValue(entityIdentity, Value);
            }

            public void Dispose()
            {
                Debug.Assert(_pool != null);
                _pool?.Recycle(this);
                _pool = null;
            }
        }
    }

    static class DynamicRawBuilderPool
    {
        private static readonly IObjectPool<PooledRawBuilder> s_pool = new ObjectPool<PooledRawBuilder, PooledRawBuilderPolicy>();

        public static PooledRawBuilder CreateBuilder(ReadOnlyMemory<byte> data, Type type)
        {
            var builder = s_pool.Rent();
            builder.Data = data;
            builder.Type = type;
            return builder;
        }

        private readonly record struct PooledRawBuilderPolicy : IObjectPoolPolicy
        {
            public int MaxCount => 32;
            public object Create() => new PooledRawBuilder();
        }

        public sealed class PooledRawBuilder : IComponentsBuilder
        {
            public ReadOnlyMemory<byte> Data { get; set; } = default!;
            public Type Type { get; set; } = default!;

            public void CollectTypes<TCollection>(TCollection types) where TCollection : ICollection<StorageType>
            {
                types.Add(StorageType.Create(Type));
            }

            public void Build(ArchetypesBuilder archetypes, Identity entityIdentity)
            {
                archetypes.SetRawData(entityIdentity, Type, Data.Span);
            }

            public void Dispose()
            {
                Data = default!;
                Type = default!;
                s_pool.Recycle(this);
            }
        }
    }

    static class DynamicBuilderPool
    {
        private static readonly IObjectPool<PooledBuilder> s_pool = new ObjectPool<PooledBuilder, PooledBuilderPolicy>();

        private readonly record struct PooledBuilderPolicy : IObjectPoolPolicy
        {
            public object Create() => new PooledBuilder(s_pool);
        }

        public static PooledBuilder CreateBuilder(object value)
        {
            var builder = s_pool.Rent();
            builder.Value = value;
            return builder;
        }

        public sealed class PooledBuilder : IComponentsBuilder
        {
            private IObjectPool<PooledBuilder>? _pool;
            public object Value { get; set; } = default!;

            public PooledBuilder(IObjectPool<PooledBuilder>? pool)
            {
                _pool = pool;
            }

            public void CollectTypes<TCollection>(TCollection types) where TCollection : ICollection<StorageType>
            {
                types.Add(StorageType.Create(Value.GetType()));
            }

            public void Build(ArchetypesBuilder archetypes, Identity entityIdentity)
            {
                archetypes.SetValue(entityIdentity, Value);
            }

            public void Dispose()
            {
                Debug.Assert(_pool != null);
                _pool?.Recycle(this);
                _pool = null;
            }
        }
    }
}

public readonly struct DefaultValueComponentBuilder<TInnerBuilder> : IComponentsBuilder
    where TInnerBuilder : IComponentsBuilder
{
    public Type ValueType { get; init; }
    public TInnerBuilder InnerBuilder { get; init; }

    public void CollectTypes<TCollection>(TCollection types) where TCollection : ICollection<StorageType>
    {
        types.Add(StorageType.Create(ValueType));
        InnerBuilder.CollectTypes(types);
    }

    public void Build(ArchetypesBuilder archetypes, Identity entityIdentity)
    {
        InnerBuilder.Build(archetypes, entityIdentity);
    }

    public void Dispose()
    {
        InnerBuilder.Dispose();
    }
}

public readonly struct ValueComponentBuilder<TValue, TInnerBuilder> : IComponentsBuilder
    where TValue : struct
    where TInnerBuilder : IComponentsBuilder
{
    public TValue Value { get; init; }
    public TInnerBuilder InnerBuilder { get; init; }

    public void CollectTypes<TCollection>(TCollection types) where TCollection : ICollection<StorageType>
    {
        types.Add(StorageType.Create<TValue>());
        InnerBuilder.CollectTypes(types);
    }

    public void Build(ArchetypesBuilder archetypes, Identity entityIdentity)
    {
        archetypes.SetValue(entityIdentity, Value);
        InnerBuilder.Build(archetypes, entityIdentity);
    }

    public void Dispose()
    {
        InnerBuilder.Dispose();
    }
}

public readonly struct NewObjectComponentBuilder<TInnerBuilder, TValue> : IComponentsBuilder
    where TInnerBuilder : IComponentsBuilder
    where TValue : class, new()
{
    public TInnerBuilder InnerBuilder { get; init; }

    public void CollectTypes<TCollection>(TCollection types) where TCollection : ICollection<StorageType>
    {
        types.Add(StorageType.Create<TValue>());
        InnerBuilder.CollectTypes(types);
    }

    public void Build(ArchetypesBuilder archetypes, Identity entityIdentity)
    {
        archetypes.CreateObject<TValue>(entityIdentity, isDuplicateAllowed: false);
        InnerBuilder.Build(archetypes, entityIdentity);
    }

    public void Dispose()
    {
        InnerBuilder.Dispose();
    }
}

public readonly struct ObjectComponentBuilder<TInnerBuilder> : IComponentsBuilder
    where TInnerBuilder : IComponentsBuilder
{
    public object? Value { get; init; }
    public TInnerBuilder InnerBuilder { get; init; }

    public void CollectTypes<TCollection>(TCollection types) where TCollection : ICollection<StorageType>
    {
        if (Value != null) types.Add(StorageType.Create(Value.GetType()));
        InnerBuilder.CollectTypes(types);
    }

    public void Build(ArchetypesBuilder archetypes, Identity entityIdentity)
    {
        if (Value != null) archetypes.SetValue(entityIdentity, Value);
        InnerBuilder.Build(archetypes, entityIdentity);
    }

    public void Dispose()
    {
        InnerBuilder.Dispose();
    }
}

public readonly struct BuilderEntityBuilder<TValue, TInnerBuilder> : IComponentsBuilder
    where TValue : IComponentsBuilder
    where TInnerBuilder : IComponentsBuilder
{
    public TValue Value { get; init; }
    public TInnerBuilder InnerBuilder { get; init; }

    public void CollectTypes<TCollection>(TCollection types) where TCollection : ICollection<StorageType>
    {
        Value.CollectTypes(types);
        InnerBuilder.CollectTypes(types);
    }

    public void Build(ArchetypesBuilder archetypes, Identity entityIdentity)
    {
        Value.Build(archetypes, entityIdentity);
        InnerBuilder.Build(archetypes, entityIdentity);
    }

    public void Dispose()
    {
        InnerBuilder.Dispose();
    }
}

public static class EntityBuilderExtension
{
    [Pure, MustUseReturnValue]
    public static ValueComponentBuilder<T, TInnerBuilder> Add<T, TInnerBuilder>(this TInnerBuilder builder, T data)
        where T : struct
        where TInnerBuilder : IComponentsBuilder
    {
        return new ValueComponentBuilder<T, TInnerBuilder> { InnerBuilder = builder, Value = data };
    }

    [Pure, MustUseReturnValue]
    public static DefaultValueComponentBuilder<TInnerBuilder> AddDefaultValue<TInnerBuilder>(this TInnerBuilder builder, Type valueType)
        where TInnerBuilder : IComponentsBuilder
    {
        return new DefaultValueComponentBuilder<TInnerBuilder> { InnerBuilder = builder, ValueType = valueType };
    }

    [Pure, MustUseReturnValue]
    public static BuilderEntityBuilder<T, TInnerBuilder> AddBuilder<T, TInnerBuilder>(this TInnerBuilder builder, T data)
        where T : IComponentsBuilder
        where TInnerBuilder : IComponentsBuilder
    {
        return new BuilderEntityBuilder<T, TInnerBuilder> { InnerBuilder = builder, Value = data };
    }

    public static Entity Build<T>(this T builder, Archetypes archetypes, Entity entity) where T : IComponentsBuilder
    {
        archetypes.BuildComponents(entity.Identity, builder);
        return entity;
    }

    public static Entity Build<T>(this T builder, World world) where T : IComponentsBuilder
    {
        return Build(builder, world.Archetypes);
    }

    public static Entity Build<T>(this T builder, Archetypes archetypes) where T : IComponentsBuilder
    {
        var entity = archetypes.Spawn();
        return builder.Build(archetypes, entity);
    }
}

public static class DynamicEntityBuilderExtension
{
    [Pure, MustUseReturnValue]
    public static NewObjectComponentBuilder<TInnerBuilder, TValue> Create<TInnerBuilder, TValue>(this TInnerBuilder builder, TValue? component)
        where TInnerBuilder : IComponentsBuilder
        where TValue : class, new()
    {
        Debug.Assert(component == null, $"use `Create(default({typeof(TValue).Name})) instead");
        return new NewObjectComponentBuilder<TInnerBuilder, TValue> { InnerBuilder = builder };
    }

    [Pure, MustUseReturnValue]
    public static ObjectComponentBuilder<TInnerBuilder> Add<TInnerBuilder>(this TInnerBuilder builder, object? component)
        where TInnerBuilder : IComponentsBuilder
    {
        return new ObjectComponentBuilder<TInnerBuilder> { Value = component, InnerBuilder = builder };
    }
}
