namespace RelEcs
{
    public static partial class WorldQueryExtension
    {
        public static Query<Entity>.Builder Query(this World world)
        {
            return new Query<Entity>.Builder(world._archetypes);
        }

        public static Query<C0>.Builder Query<C0>(this World world) where C0 : class
        {
            return new Query<C0>.Builder(world._archetypes);
        }
    }
}
