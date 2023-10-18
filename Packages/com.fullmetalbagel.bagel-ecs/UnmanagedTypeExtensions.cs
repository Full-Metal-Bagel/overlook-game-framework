using System;
using System.Diagnostics.CodeAnalysis;

namespace RelEcs
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    [SuppressMessage("Design", "CA1812:Internal class that is apparently never instantiated")]
    internal static class UnmanagedTypeExtensions
    {
        private sealed class U<T> where T : unmanaged { }
        public static bool IsUnmanaged(this Type t)
        {
            try { typeof(U<>).MakeGenericType(t); return true; }
            catch (Exception) { return false; }
        }
    }
}
