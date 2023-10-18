using System.Collections.Generic;

namespace RelEcs.Tests
{
    internal static class Extensions
    {
        public static int Count(this Query query)
        {
            int count = 0;
            foreach (var _ in query) count++;
            return count;
        }

        public static IEnumerable<Entity> AsEnumerable(this Query query)
        {
            var result = new List<Entity>();
            foreach (var entity in query) result.Add(entity);
            return result;
        }

        public static IEnumerable<T> AsEnumerable<T>(this Query query) where T : class
        {
            var result = new List<T>();
            foreach (var entity in query) result.Add(entity.Get<T>());
            return result;
        }
    }
}
