using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RelEcs
{
    public sealed class World
    {
        static int worldCount;

        internal readonly Archetypes _archetypes = new();
        private int Id { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public World()
        {
            Id = Interlocked.Increment(ref worldCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder Spawn()
        {
            return new EntityBuilder(this, _archetypes.Spawn());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder On(Entity entity)
        {
            return new EntityBuilder(this, entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Despawn(Entity entity)
        {
            _archetypes.Despawn(entity.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DespawnAllWith<T>() where T : class
        {
            var query = this.Query<Entity>().Has<T>().Build();
            foreach (var entity in query) Despawn(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(Entity? entity)
        {
            return entity is not null && _archetypes.IsAlive(entity.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetComponent<T>(Entity entity) where T : class
        {
            var type = StorageType.Create<T>(Identity.None);
            return (T)_archetypes.GetComponent(type, entity.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetComponent<T>(Entity entity, out T? component) where T : class
        {
            var type = StorageType.Create<T>(Identity.None);
            if (!HasComponent<T>(entity))
            {
                component = null;
                return false;
            }

            component = (T)_archetypes.GetComponent(type, entity.Identity);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(Entity entity) where T : class
        {
            var type = StorageType.Create<T>(Identity.None);
            return _archetypes.HasComponent(type, entity.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T AddComponent<T>(Entity entity) where T : class, new()
        {
            var component = new T();
            return AddComponent(entity, component);
        }

        public T AddComponent<T>(Entity entity, [DisallowNull] T component)
        {
            var type = StorageType.Create(component.GetType(), Identity.None);
            _archetypes.AddComponent(type, entity.Identity, component);
            return component;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(Entity entity)
        {
            var type = StorageType.Create<T>(Identity.None);
            _archetypes.RemoveComponent(type, entity.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<(StorageType, object?)> GetComponents(Entity entity)
        {
            return _archetypes.GetComponents(entity.Identity);
        }

        public void GetComponents<T>(Entity entity, ICollection<T> components)
        {
            _archetypes.GetComponents(entity.Identity, components);
        }

        public void GetComponents(Entity entity, ICollection<object> components)
        {
            _archetypes.GetComponents(entity.Identity, components);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetComponent<T>(Entity entity, Entity target) where T : class
        {
            var type = StorageType.Create<T>(target.Identity);
            return (T)_archetypes.GetComponent(type, entity.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetComponent<T>(Entity entity, out T? component, Entity target) where T : class
        {
            var type = StorageType.Create<T>(target.Identity);
            if (!HasComponent<T>(entity, target))
            {
                component = null;
                return false;
            }

            component = (T)_archetypes.GetComponent(type, entity.Identity);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(Entity entity, Entity target) where T : class
        {
            var type = StorageType.Create<T>(target.Identity);
            return _archetypes.HasComponent(type, entity.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(Entity entity, Entity target) where T : class, new()
        {
            var type = StorageType.Create<T>(target.Identity);
            _archetypes.AddComponent(type, entity.Identity, new T());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(Entity entity, T component, Entity target) where T : class
        {
            var type = StorageType.Create<T>(target.Identity);
            _archetypes.AddComponent(type, entity.Identity, component);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(Entity entity, Entity target) where T : class
        {
            var type = StorageType.Create<T>(target.Identity);
            _archetypes.RemoveComponent(type, entity.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity GetTarget<T>(Entity entity) where T : class
        {
            var type = StorageType.Create<T>(Identity.None);
            return _archetypes.GetTarget(type, entity.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Entity> GetTargets<T>(Entity entity) where T : class
        {
            var type = StorageType.Create<T>(Identity.None);
            return _archetypes.GetTargets(type, entity.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Entity GetTypeEntity(Type type)
        {
            return _archetypes.GetTypeEntity(type);
        }
    }
}
