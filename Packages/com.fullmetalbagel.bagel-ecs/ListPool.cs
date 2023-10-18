using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace RelEcs
{
    public static class ListPool<T>
    {
        private static readonly Stack<List<T>> s_stack = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> Get()
        {
            return s_stack.Count > 0 ? s_stack.Pop() : new List<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(List<T> list)
        {
            list.Clear();
            s_stack.Push(list);
        }
    }
}
