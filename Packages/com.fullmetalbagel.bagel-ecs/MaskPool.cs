using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace RelEcs
{
    public static class MaskPool
    {
        private static readonly Stack<Mask> s_stack = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mask Get()
        {
            return s_stack.Count > 0 ? s_stack.Pop() : new Mask();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(Mask list)
        {
            list.Clear();
            s_stack.Push(list);
        }
    }
}
