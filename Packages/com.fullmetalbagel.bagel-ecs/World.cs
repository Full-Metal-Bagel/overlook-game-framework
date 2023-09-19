using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RelEcs
{
    public sealed class World
    {
        static int worldCount;

        internal readonly Entity _world;
        internal readonly WorldInfo _worldInfo;

        internal readonly Archetypes _archetypes = new();

        public WorldInfo Info => _worldInfo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public World()
        {
            _world = _archetypes.Spawn();
            _worldInfo = new WorldInfo(worldCount);
            Interlocked.Increment(ref worldCount);
            _archetypes.AddComponent(StorageType.Create<WorldInfo>(Identity.None), _world.Identity, _worldInfo);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T AddComponent<T>(Entity entity, T component) where T : class
        {
            var type = StorageType.Create<T>(Identity.None);
            _archetypes.AddComponent(type, entity.Identity, component);
            return component;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object AddObjectComponent(Entity entity, object component)
        {
            var type = StorageType.Create(component.GetType(), Identity.None);
            _archetypes.AddComponent(type, entity.Identity, component);
            return component;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(Entity entity) where T : class
        {
            var type = StorageType.Create<T>(Identity.None);
            _archetypes.RemoveComponent(type, entity.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<(StorageType, object?)> GetComponents(Entity entity)
        {
            return _archetypes.GetComponents(entity.Identity);
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
        public T GetElement<T>() where T : class
        {
            var type = StorageType.Create<T>(Identity.None);
            return (T)_archetypes.GetComponent(type, _world.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetElement<T>(out T? element) where T : class
        {
            var type = StorageType.Create<T>(Identity.None);
            if (!HasElement<T>())
            {
                element = null;
                return false;
            }

            element = (T)_archetypes.GetComponent(type, _world.Identity);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasElement<T>() where T : class
        {
            var type = StorageType.Create<T>(Identity.None);
            return _archetypes.HasComponent(type, _world.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddElement<T>(T element) where T : class
        {
            var type = StorageType.Create<T>(Identity.None);
            _archetypes.AddComponent(type, _world.Identity, element);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReplaceElement<T>(T element) where T : class
        {
            var type = StorageType.Create<T>(Identity.None);
            _archetypes.RemoveComponent(type, _world.Identity);
            _archetypes.AddComponent(type, _world.Identity, element);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddOrReplaceElement<T>(T element) where T : class
        {
            var type = StorageType.Create<T>(Identity.None);

            if (_archetypes.HasComponent(type, _world.Identity))
            {
                _archetypes.RemoveComponent(type, _world.Identity);
            }

            _archetypes.AddComponent(type, _world.Identity, element);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveElement<T>() where T : class
        {
            var type = StorageType.Create<T>(Identity.None);
            _archetypes.RemoveComponent(type, _world.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tick()
        {
            _worldInfo.EntityCount = _archetypes.EntityCount;
            _worldInfo.UnusedEntityCount = _archetypes.UnusedIds.Count;
            _worldInfo.AllocatedEntityCount = _archetypes.Meta.Length;
            _worldInfo.ArchetypeCount = _archetypes.Tables.Count;
            // info.RelationCount = relationCount;
            _worldInfo.ElementCount = _archetypes.Tables[_archetypes.Meta[_world.Identity.Id].TableId].Types.Count;
            _worldInfo.CachedQueryCount = _archetypes.Queries.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Entity GetTypeEntity(Type type)
        {
            return _archetypes.GetTypeEntity(type);
        }
    }

    public sealed class WorldInfo
    {
        public readonly int WorldId;
        public int EntityCount;
        public int UnusedEntityCount;
        public int AllocatedEntityCount;

        public int ArchetypeCount;

        // public int RelationCount;
        public int ElementCount;
        public int CachedQueryCount;

        public WorldInfo(int id)
        {
            WorldId = id;
        }
    }
}
