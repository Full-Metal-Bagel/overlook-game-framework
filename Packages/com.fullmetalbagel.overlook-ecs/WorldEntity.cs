using System;
using System.Collections.Generic;

#if OVERLOOK_ECS_USE_UNITY_COLLECTION
using TSetEnumerator = Overlook.Ecs.NativeBitArraySet.Enumerator;
#else
using TSetEnumerator = System.Collections.Generic.SortedSet<Overlook.Ecs.StorageType>.Enumerator;
#endif

namespace Overlook.Ecs;

public readonly record struct WorldEntity(World World, Entity Entity)
{
    public static WorldEntity None => new(null!, Entity.None);

    public bool IsValid => !Entity.IsNone && World != null && World.IsAlive(Entity);
    public bool IsInvalid => !IsValid;

    public WorldEntity Spawn() => new(World, World.Spawn());

    public Span<byte> GetRawData(Type componentType) => World.GetComponentRawData(Entity, StorageType.Create(componentType));

    public ref T Get<T>() where T : unmanaged => ref World.GetComponent<T>(Entity);
    public bool TryGet<T>(out T? component) where T : unmanaged => World.TryGetComponent(Entity, out component);

    public object GetObject(Type type) => World.GetComponent(Entity, type);
    public T GetObject<T>() where T : class => World.GetComponent<T>(Entity);

    public void GetObjects<T>(ICollection<T> collection) where T : class
        => World.FindObjectComponents(this, collection);
    public bool TryGetObject<T>(out T? component) where T : class => World.TryGetComponent(Entity, out component);

    public void RemoveObject<T>() where T : class => World.RemoveObjectComponent<T>(Entity);

    public void TryAdd<T>(T component = default) where T : unmanaged
    {
        if (Has<T>()) return;
        Add(component);
    }
    public void Add<T>(T component = default) where T : unmanaged => World.AddComponent(Entity, component);

    public void Add(Type componentType) => World.AddDefaultComponent(Entity, componentType);

    public void TryRemove<T>(T component = default) where T : unmanaged
    {
        if (!Has<T>()) return;
        Remove(component);
    }
    public void Remove<T>(T component = default) where T : unmanaged => World.RemoveComponent<T>(Entity);
    public void Remove(Type componentType) => World.RemoveComponent(Entity, componentType);
    public T AddMultipleObject<T>() where T : class, new() => World.AddMultipleObjectComponent<T>(Entity);
    public T AddMultipleObject<T>(T component) where T : class => World.AddMultipleObjectComponent(Entity, component);
    public T AddObject<T>() where T : class, new() => World.AddObjectComponent<T>(Entity);
    public T AddObject<T>(T component) where T : class => World.AddObjectComponent(Entity, component);

    public bool Has<T>() => World.HasComponent<T>(Entity);
    public bool Has(Type type) => World.HasComponent(Entity, type);

    public void AddTaggedComponent<T>(T component, Type tagType) where T : class
    {
        World.AddTaggedComponent(Entity, component, tagType);
    }

    public T? FindUnwrappedComponent<T>(Type tagGenericDefinition) where T : class =>
        World.FindUnwrappedComponent<T>(Entity, tagGenericDefinition);

    public void FindUnwrappedComponents<T>(ICollection<T> results, Type tagGenericDefinition) where T : class =>
        World.FindUnwrappedComponents<T>(Entity, results, tagGenericDefinition);

    public override string ToString()
    {
        return Entity.ToString();
    }

    // ReSharper disable once NotDisposedResourceIsReturned
    public TSetEnumerator GetEnumerator() => World.Archetypes.GetTableTypes(Entity.Identity).GetEnumerator();

    public static implicit operator Entity(WorldEntity gameEntity) => gameEntity.Entity;
}

public static class WorldEntityExtension
{
    public static T Get<T>(this in WorldEntity entity) where T : class
    {
        return entity.GetObject<T>();
    }

    public static void Remove<T>(this in WorldEntity entity) where T : class
    {
        entity.RemoveObject<T>();
    }

    public static bool TryGet<T>(this in WorldEntity entity, out T? component) where T : class
    {
        return entity.TryGetObject(out component);
    }

    public static T Add<T>(this in WorldEntity entity) where T : class, new()
    {
        return entity.AddObject<T>();
    }

    public static T Add<T>(this in WorldEntity entity, T component) where T : class
    {
        return entity.AddObject(component);
    }

    public static T AddMultiple<T>(this in WorldEntity entity) where T : class, new()
    {
        return entity.AddMultipleObject<T>();
    }

    public static T AddMultiple<T>(this in WorldEntity entity, T component) where T : class
    {
        return entity.AddMultipleObject(component);
    }

    public static WorldEntity AsWorldEntity(this Entity entity, World world)
    {
        return new WorldEntity(world, entity);
    }

    public static WorldEntity Build<T>(this T builder, in WorldEntity entity) where T : IComponentsBuilder
    {
        entity.World.Archetypes.BuildComponents(entity.Entity.Identity, builder);
        return entity;
    }

    public static WorldEntity Build<T>(this T builder, World world) where T : IComponentsBuilder
    {
        var entity = world.Spawn().AsWorldEntity(world);
        return builder.Build(entity);
    }
}
