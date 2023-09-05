using System.Collections;
using System.Collections.Generic;

namespace RelEcs.Tests
{
    public static class Extensions
    {
        public static int Count(this IEnumerator enumerator)
        {
            int count = 0;
            while (enumerator.MoveNext())
            {
                count++;
            }
            return count;
        }

        public static IEnumerable<C1> AsEnumerable<C1>(this Enumerator<C1> enumerator)
            where C1 : class
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        public static IEnumerable<(C1, C2)> AsEnumerable<C1, C2>(this Enumerator<C1, C2> enumerator)
            where C1 : class
            where C2 : class
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }
}
