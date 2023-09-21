using System;
using System.Runtime.CompilerServices;

namespace RelEcs
{
    public sealed class Entity
    {
        public static readonly Entity None = new(Identity.None);
        public static readonly Entity Any = new(Identity.Any);

        public bool IsAny => Identity == Identity.Any;
        public bool IsNone => Identity == Identity.None;

        public Identity Identity { get; }

        public Entity(Identity identity)
        {
            Identity = identity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is Entity entity && Identity.Equals(entity.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return Identity.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return Identity.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Entity? left, Entity? right)
        {
            if (left is null && right is null) return true;
            if (left != null && right != null) return left.Equals(right);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Entity? left, Entity? right)
        {
            if (left is null && right is null) return false;
            if (left != null && right != null) return !left.Equals(right);
            return true;
        }
    }

    public readonly struct EntityBuilder
    {
        internal readonly World World;
        readonly Entity _entity;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder(World world, Entity entity)
        {
            World = world;
            _entity = entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder Add<T>() where T : class, new()
        {
            World.AddComponent<T>(_entity);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder Add<T>(T data) where T : class
        {
            World.AddComponent(_entity, data);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder Remove<T>() where T : class
        {
            World.RemoveComponent<T>(_entity);
            return this;
        }

        public Entity Id()
        {
            return _entity;
        }
    }
}
