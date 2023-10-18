using System;

namespace RelEcs
{
    public readonly struct EntityMeta : IEquatable<EntityMeta>
    {
        public Identity Identity { get; }
        public int TableId { get; }
        public int Row { get; }

        public EntityMeta(in Identity identity, int tableId, int row)
        {
            Identity = identity;
            TableId = tableId;
            Row = row;
        }

        public override bool Equals(object? obj)
        {
            return obj is EntityMeta other && other.Equals(this);
        }

        public bool Equals(EntityMeta other)
        {
            return Identity.Equals(other.Identity) && TableId == other.TableId && Row == other.Row;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Identity, TableId, Row);
        }

        public static bool operator ==(in EntityMeta left, in EntityMeta right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(in EntityMeta left, in EntityMeta right)
        {
            return !(left == right);
        }
    }

    public readonly struct Identity : IEquatable<Identity>
    {
        public static Identity None;
        public static Identity Any = new(int.MaxValue, 0);

        public int Id { get; }
        public ushort Generation { get; }

        public Identity(int id, ushort generation = 1)
        {
            Id = id;
            Generation = generation;
        }

        public override bool Equals(object? obj)
        {
            return (obj is Identity other) && other.Equals(this);
        }

        public bool Equals(Identity other)
        {
            return Id == other.Id && Generation == other.Generation;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Generation);
        }

        public override string ToString()
        {
            return $"{Id}({Generation})";
        }

        public static bool operator ==(Identity left, Identity right) => left.Equals(right);
        public static bool operator !=(Identity left, Identity right) => !left.Equals(right);
    }
}
