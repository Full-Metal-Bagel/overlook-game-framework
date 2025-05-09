﻿using System;
using System.Collections.Generic;

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
}
