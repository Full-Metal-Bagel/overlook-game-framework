namespace RelEcs
{
    public readonly struct Entity : System.IEquatable<Entity>
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

        public override bool Equals(object? obj)
        {
            return obj is Entity entity && entity.Equals(this);
        }

        public bool Equals(Entity entity)
        {
            return Identity.Equals(entity.Identity);
        }

        public override int GetHashCode()
        {
            return Identity.GetHashCode();
        }

        public override string ToString()
        {
            return Identity.ToString();
        }

        public static bool operator ==(in Entity left, in Entity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(in Entity left, in Entity right)
        {
            return !left.Equals(right);
        }
    }

    public readonly ref struct EntityBuilder
    {
        internal World World { get; }
        internal Entity Entity { get; }

        public EntityBuilder(World world, Entity entity)
        {
            World = world;
            Entity = entity;
        }

        public EntityBuilder Add<T>(T data = default) where T : struct
        {
            World.AddComponent(Entity, data);
            return this;
        }

        public Entity Id()
        {
            return Entity;
        }
    }

    public static partial class ObjectComponentExtension
    {
        public static EntityBuilder Add<T>(this in EntityBuilder builder) where T : class, new()
        {
            return Add(builder, new T());
        }

        public static EntityBuilder Add<T>(this in EntityBuilder builder, T component) where T : class
        {
            if (component.GetType().IsValueType) AddUntypedValueComponent(builder, component);
            else builder.World.AddComponent(builder.Entity, component);
            return builder;
        }

        public static EntityBuilder AddMultiple<T>(this in EntityBuilder builder, T component) where T : class
        {
            builder.World.AddMultipleObjectComponent(builder.Entity, component);
            return builder;
        }

        public static EntityBuilder AddUntypedValueComponent(this in EntityBuilder builder, object component)
        {
            builder.World.Archetypes.AddUntypedValueComponent(builder.Entity.Identity, component);
            return builder;
        }
    }
}
