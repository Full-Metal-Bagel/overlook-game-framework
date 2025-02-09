using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Unity.Collections
{
    public enum Allocator
    {
        Invalid = 0,
        None = 1,
        Temp = 2,
        TempJob = 3,
        Persistent = 4,
        AudioKernel = 5,
        FirstUserIndex = 64, // 0x00000040
    }
}

namespace UnityEngine
{
    public enum LogType
    {
        Error,
        Assert,
        Warning,
        Log,
        Exception,
    }

    [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
    [SuppressMessage("Style", "IDE1006:Naming Styles")]
    public class Object
    {
        public string name { get; set; } = "";
    }

    public class GameObject : Object { }

    public static class Debug
    {
        public static void Log(object message, Object? _ = null)
        {
            System.Console.WriteLine(message);
        }

        public static void LogWarning(object message, Object? _ = null)
        {
            System.Console.WriteLine($"Warning: {message}");
        }

        public static void LogError(object message, Object? _ = null)
        {
            System.Console.WriteLine($"Error: {message}");
        }

        public static void LogException(System.Exception exception, Object? _ = null)
        {
            System.Console.WriteLine($"Exception: {exception}");
        }

        public static void Assert(bool condition, Object? _ = null)
        {
            System.Diagnostics.Debug.Assert(condition);
        }

        public static void Assert(bool condition, string message, Object? _ = null)
        {
            System.Diagnostics.Debug.Assert(condition, message);
        }

        public static void AssertFormat(bool condition, string message, Object? _ = null)
        {
            System.Diagnostics.Debug.Assert(condition, message);
        }
    }
}
