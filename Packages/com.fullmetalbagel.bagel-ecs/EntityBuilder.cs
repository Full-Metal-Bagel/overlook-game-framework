using System.Collections.Generic;
using Game;
using JetBrains.Annotations;

namespace RelEcs
{
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
            private static readonly UnityEngine.Pool.ObjectPool<PooledBuilder> s_pool = new(
                createFunc: () => new PooledBuilder { Pool = s_pool },
                actionOnGet: x =>
                {
                    x.Pool = s_pool;
                    x.Value = default!;
                },
                actionOnRelease: x =>
                {
                    x.Pool = null;
                    x.Value = default!;
                },
                defaultCapacity: 4
            );

            public static PooledBuilder CreateBuilder(T value)
            {
                var builder = s_pool.Get();
                builder.Value = value;
                return builder;
            }

            public sealed class PooledBuilder : IComponentsBuilder
            {
                public UnityEngine.Pool.ObjectPool<PooledBuilder>? Pool { get; set; }
                public T Value { get; set; } = default!;

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
                    Debug.Assert(Pool != null);
                    Pool?.Release(this);
                    Pool = null;
                }
            }
        }

        static class DynamicBuilderPool
        {
            private static readonly UnityEngine.Pool.ObjectPool<PooledBuilder> s_pool = new(
                createFunc: () => new PooledBuilder { Pool = s_pool },
                actionOnGet: x =>
                {
                    x.Pool = s_pool;
                    x.Value = default!;
                },
                actionOnRelease: x =>
                {
                    x.Pool = null;
                    x.Value = default!;
                },
                defaultCapacity: 32
            );

            public static PooledBuilder CreateBuilder(object value)
            {
                var builder = s_pool.Get();
                builder.Value = value;
                return builder;
            }

            public sealed class PooledBuilder : IComponentsBuilder
            {
                public UnityEngine.Pool.ObjectPool<PooledBuilder>? Pool { get; set; }
                public object Value { get; set; } = default!;

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
                    Debug.Assert(Pool != null);
                    Pool?.Release(this);
                    Pool = null;
                }
            }
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
        public static ObjectComponentBuilder<TInnerBuilder> Add<TInnerBuilder>(this TInnerBuilder builder, object? component)
            where TInnerBuilder : IComponentsBuilder
        {
            return new ObjectComponentBuilder<TInnerBuilder> { Value = component, InnerBuilder = builder };
        }
    }
}
