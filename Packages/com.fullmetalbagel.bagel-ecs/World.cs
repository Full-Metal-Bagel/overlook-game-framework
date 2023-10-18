using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Game;

namespace RelEcs
{
    public sealed class World
    {
        private static int s_worldCount;

        internal Archetypes Archetypes { get; } = new();
        private int Id { get; }

        public World()
        {
            Id = Interlocked.Increment(ref s_worldCount);
        }

        public EntityBuilder Spawn()
        {
            return new EntityBuilder(this, Archetypes.Spawn());
        }

        public EntityBuilder On(Entity entity)
        {
            return new EntityBuilder(this, entity);
        }

        public void Despawn(Entity entity)
        {
            Archetypes.Despawn(entity.Identity);
        }

        public void DespawnAllWith<T>()
        {
            var query = this.Query().Has<T>().Build();
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

        public T GetObjectComponent<T>(Entity entity) where T : class
        {
            return Archetypes.GetObjectComponent<T>(entity.Identity);
        }

        public bool TryGetObjectComponent<T>(Entity entity, out T? component) where T : class
        {
            if (!HasComponent<T>(entity))
            {
                component = null;
                return false;
            }

            component = Archetypes.GetObjectComponent<T>(entity.Identity);
            return true;
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

        public bool HasComponent<T>(Entity entity)
        {
            var type = StorageType.Create<T>();
            return Archetypes.HasComponent(type, entity.Identity);
        }

        public ref T AddComponent<T>(Entity entity, T component = default) where T : struct
        {
            return ref Archetypes.AddComponent(entity.Identity, component);
        }

        public T AddObjectComponent<T>(Entity entity) where T : class, new()
        {
            return AddObjectComponent(entity, new T());
        }

        public T AddObjectComponent<T>(Entity entity, [DisallowNull] T component) where T : class
        {
            Archetypes.AddObjectComponent(entity.Identity, component);
            return component;
        }

        public void RemoveComponent<T>(Entity entity)
        {
            var type = StorageType.Create<T>();
            Archetypes.RemoveComponent(type, entity.Identity);
        }

        public Query.Builder Query()
        {
            return new Query.Builder(Archetypes);
        }
    }

    public static partial class ObjectComponentExtension
    {
        public static T AddComponent<T>(this World world, Entity entity) where T : class, new()
        {
            return world.AddObjectComponent(entity, new T());
        }

        public static T AddComponent<T>(this World world, Entity entity, [DisallowNull] T component) where T : class
        {
            return world.AddObjectComponent(entity, component);
        }

        public static T GetComponent<T>(this World world, Entity entity) where T : class
        {
            return world.GetObjectComponent<T>(entity);
        }

        public static bool TryGetComponent<T>(this World world, Entity entity, out T? component) where T : class
        {
            return world.TryGetObjectComponent(entity, out component);
        }
    }
}
