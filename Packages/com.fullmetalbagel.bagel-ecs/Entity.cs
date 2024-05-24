namespace RelEcs
{
    public readonly struct Entity : System.IEquatable<Entity>
    {
        public static readonly Entity None = new(Identity.None);
        public static readonly Entity Any = new(Identity.Any);
        public static readonly EntityBuilder Builder = default;

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
}
