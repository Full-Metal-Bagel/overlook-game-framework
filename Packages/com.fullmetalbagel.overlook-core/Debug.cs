using System;
using System.Diagnostics;

namespace Overlook;

public static class Debug
{
    #region Assert

    [Conditional("OVERLOOK_DEBUG")]
    public static void Assert(bool condition)
    {
        UnityEngine.Debug.Assert(condition);
    }

    [Conditional("OVERLOOK_DEBUG")]
    public static void Assert(bool condition, object? context)
    {
        UnityEngine.Debug.Assert(condition, context as UnityEngine.Object);
    }

    [Conditional("OVERLOOK_DEBUG")]
    public static void Assert(bool condition, string message)
    {
        UnityEngine.Debug.Assert(condition, message);
    }

    [Conditional("OVERLOOK_DEBUG")]
    public static void Assert(bool condition, string message, object? context)
    {
        UnityEngine.Debug.Assert(condition, message, context as UnityEngine.Object);
    }

    #endregion

    #region Logger

    [Conditional("OVERLOOK_DEBUG")]
    public static void Log(object message)
    {
        UnityEngine.Debug.Log(message);
    }

    [Conditional("OVERLOOK_DEBUG")]
    public static void Log(object message, object? context)
    {
        UnityEngine.Debug.Log(message, context as UnityEngine.Object);
    }

    [Conditional("OVERLOOK_DEBUG")]
    public static void LogWarning(object message)
    {
        UnityEngine.Debug.LogWarning(message);
    }

    [Conditional("OVERLOOK_DEBUG")]
    public static void LogWarning(object message, object? context)
    {
        UnityEngine.Debug.LogWarning(message, context as UnityEngine.Object);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional("OVERLOOK_DEBUG")]
    public static void LogError(object message)
    {
        UnityEngine.Debug.LogError(message);
    }

    [Conditional("OVERLOOK_DEBUG")]
    public static void LogError(object message, object? context)
    {
        UnityEngine.Debug.LogError(message, context as UnityEngine.Object);
    }

    public static void LogException(Exception ex)
    {
        UnityEngine.Debug.LogException(ex);
    }

    public static void LogException(Exception ex, object? context)
    {
        UnityEngine.Debug.LogException(ex, context as UnityEngine.Object);
    }

    #endregion
}