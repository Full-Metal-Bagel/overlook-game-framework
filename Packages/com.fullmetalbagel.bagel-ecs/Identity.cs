namespace RelEcs
{
    public readonly record struct EntityMeta(Identity Identity, int TableId, int Row)
    {
        public static EntityMeta Invalid => new(Identity.None, -1, -1);
    }

    public readonly record struct Identity(int Id, int Generation = 1)
    {
        public static Identity None = new(0, 0);
        public static Identity Any = new(int.MaxValue, 0);
        public override string ToString() => $"{Id}({Generation})";
    }
}
