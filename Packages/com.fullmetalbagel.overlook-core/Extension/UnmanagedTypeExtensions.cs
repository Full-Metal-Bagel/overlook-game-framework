using System;
using System.Diagnostics.CodeAnalysis;
#if !UNITY_5_3_OR_NEWER
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
#endif

namespace Overlook;

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
[SuppressMessage("Design", "CA1812:Internal class that is apparently never instantiated")]
public static class UnmanagedTypeExtensions
{
    // https://stackoverflow.com/a/53969223
#if !UNITY_5_3_OR_NEWER
    private static readonly ConcurrentDictionary<Type, bool> s_memoized = new();
#endif

    public static bool IsUnmanaged(this Type type)
    {
#if UNITY_5_3_OR_NEWER
        return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.IsUnmanaged(type);
#else
        bool answer;

        // check if we already know the answer
        if (!s_memoized.TryGetValue(type, out answer))
        {
            if (!type.IsValueType)
            {
                // not a struct -> false
                answer = false;
            }
            else if (type.IsPrimitive || type.IsPointer || type.IsEnum)
            {
                // primitive, pointer or enum -> true
                answer = true;
            }
            else
            {
                // otherwise check recursively
                answer = type
                    .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .All(f => IsUnmanaged(f.FieldType));
            }

            s_memoized[type] = answer;
        }

        return answer;
#endif
    }
}
