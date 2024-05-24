using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace RelEcs
{
    public sealed class World : IDisposable
    {
        private static int s_worldCount;

        internal Archetypes Archetypes { get; } = new();
        private int Id { get; }

        public World()
        {
            Id = Interlocked.Increment(ref s_worldCount);
        }

        public Entity Spawn()
        {
            return Archetypes.Spawn();
        }

        public void Despawn(Entity entity)
        {
            Archetypes.Despawn(entity.Identity);
        }

        public void DespawnAllWith<T>()
        {
            var query = QueryBuilder.Create().Has<T>().Build(this);
            foreach (var entity in query) Despawn(entity);
        }

        public bool IsAlive(Entity entity)
        {
            return Archetypes.IsAlive(entity.Identity);
        }

        public ref T GetComponent<T>(Entity entity) where T : struct
        {
            return ref Archetypes.GetComponent<T>(entity.Identity);
        }

        public Span<byte> GetComponentRawData(Entity entity, StorageType type)
        {
            return Archetypes.GetComponentRawData(entity.Identity, type);
        }

        public void SetComponentRawData(Entity entity, StorageType type, Span<byte> data)
        {
            Archetypes.SetComponentRawData(entity.Identity, type, data);
        }

        public T GetObjectComponent<T>(Entity entity) where T : class
        {
            return Archetypes.GetObjectComponent<T>(entity.Identity);
        }

        public bool TryGetObjectComponent<T>(Entity entity, out T? component) where T : class
        {
            return Archetypes.TryGetObjectComponent(entity.Identity, out component);
        }

        public bool TryGetComponent<T>(Entity entity, out T? component) where T : struct
        {
            if (!HasComponent<T>(entity))
            {
                component = null;
                return false;
            }

            component = Archetypes.GetComponent<T>(entity.Identity);
            return true;
        }

        public bool HasComponent(Entity entity, Type type)
        {
            return Archetypes.HasComponent(StorageType.Create(type), entity.Identity);
        }

        public bool HasComponent<T>(Entity entity)
        {
            var type = StorageType.Create<T>();
            return Archetypes.HasComponent(type, entity.Identity);
        }

        public void AddComponent<T>(Entity entity, T component = default) where T : struct
        {
            Archetypes.AddComponent(entity.Identity, component);
        }

        public T AddObjectComponent<T>(Entity entity) where T : class, new()
        {
            return AddObjectComponent(entity, new T());
        }

        public T AddObjectComponent<T>(Entity entity, [DisallowNull] T component) where T : class
        {
            return Archetypes.AddObjectComponent(entity.Identity, component);
        }

        public T AddMultipleObjectComponent<T>(Entity entity, [DisallowNull] T component) where T : class
        {
            return Archetypes.AddMultipleObjectComponent(entity.Identity, component);
        }

        public void RemoveComponent<T>(Entity entity) where T : struct
        {
            Archetypes.RemoveComponent<T>(entity.Identity);
        }

        public void RemoveObjectComponent<T>(Entity entity) where T : class
        {
            Archetypes.RemoveObjectComponent<T>(entity.Identity);
        }

        public void RemoveComponent(Entity entity, Type type)
        {
            Archetypes.RemoveComponent(entity.Identity, type);
        }

        public void Dispose() => Archetypes.Dispose();
    }

    public static partial class ObjectComponentExtension
    {
        public static T AddObjectComponent<T>(this World world, Entity entity) where T : class, new()
        {
            return world.AddObjectComponent(entity, new T());
        }

        public static T AddComponent<T>(this World world, Entity entity) where T : class, new()
        {
            return world.AddObjectComponent(entity, new T());
        }

        public static T AddComponent<T>(this World world, Entity entity, [DisallowNull] T component) where T : class
        {
            return world.AddObjectComponent(entity, component);
        }

        public static void RemoveComponent<T>(this World world, Entity entity) where T : class
        {
            world.RemoveObjectComponent<T>(entity);
        }

        public static T GetComponent<T>(this World world, Entity entity) where T : class
        {
            return world.GetObjectComponent<T>(entity);
        }

        public static bool TryGetComponent<T>(this World world, Entity entity, out T? component) where T : class
        {
            return world.TryGetObjectComponent(entity, out component);
        }

        public static void FindObjectComponents<T>(this World world, Entity entity, ICollection<T> collection) where T : class
        {
            world.Archetypes.FindObjectComponents(entity.Identity, collection);
        }
    }
}
