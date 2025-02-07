using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Overlook.Ecs.Tests
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

        public static IEnumerable<Entity> AsEnumerable<TEnumerator>(this IQuery<TEnumerator, QueryEntity> query)
            where TEnumerator : IQueryEnumerator<QueryEntity>
        {
            var result = new List<Entity>();
            foreach (var entity in query) result.Add(entity);
            return result;
        }

        public static IEnumerable<T> AsEnumerable<T, TQuery, TEnumerator>(this TQuery query)
            where T : class
            where TQuery : IQuery<TEnumerator, QueryEntity>
            where TEnumerator : struct, IQueryEnumerator<QueryEntity>
        {
            var result = new List<T>();
            foreach (var entity in query) result.Add(entity.Get<T>());
            return result;
        }
    }

    internal static class AssertUtils
    {
        public static void ExpectDebugAssert(TestDelegate code, int assertionTimes = 1)
        {
#if UNITY_2022_3_OR_NEWER
            while (assertionTimes > 0)
            {
                UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Assert, new Regex(".*"));
                assertionTimes--;
            }
#endif
            code();
        }

        public static void CatchDebugAssert(TestDelegate code)
        {
#if UNITY_2022_3_OR_NEWER
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Assert, new Regex(".*"));
#endif
            Assert.Catch(code);
        }
    }
}
