using System.Collections;
using System.Collections.Generic;

namespace RelEcs.Tests
{
    public static class Extensions
    {
        public static int Count<C0>(this Query<C0> query) where C0 : class
        {
            int count = 0;
            foreach (var _ in query) count++;
            return count;
        }

        public static int Count<C0, C1>(this Query<C0, C1> query)
            where C0 : class
            where C1 : class
        {
            int count = 0;
            foreach (var _ in query) count++;
            return count;
        }

        public static IEnumerable<C0> AsEnumerable<C0>(this Query<C0> query)
            where C0 : class
        {
            var result = new List<C0>();
            foreach (var entity in query) result.Add(entity);
            return result;
        }

        public static IEnumerable<(C1, C2)> AsEnumerable<C1, C2>(this Query<C1, C2> query)
            where C1 : class
            where C2 : class
        {
            var result = new List<(C1, C2)>();
            foreach (var entity in query) result.Add(entity);
            return result;
        }
    }
}
