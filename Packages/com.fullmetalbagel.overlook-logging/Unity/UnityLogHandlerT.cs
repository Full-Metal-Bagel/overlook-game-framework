using System;
using UnityEngine;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// A generic implementation of <see cref="ILogger{TCategoryName}"/> that wraps Unity's <see cref="ILogHandler"/>.
/// The category name is derived from the type parameter.
/// </summary>
/// <typeparam name="TCategoryName">The type whose name is used for the logger category name.</typeparam>
public sealed class UnityLogHandler<TCategoryName> : ILogger<TCategoryName>
{
    private readonly UnityLogHandler _inner;

    /// <summary>
    /// Creates a new instance of <see cref="UnityLogHandler{TCategoryName}"/>.
    /// </summary>
    /// <param name="logHandler">The Unity log handler to wrap.</param>
    /// <param name="minLevel">The minimum log level to output. Defaults to <see cref="LogLevel.Trace"/>.</param>
    /// <param name="context">Optional Unity Object context for log messages.</param>
    public UnityLogHandler(ILogHandler logHandler, LogLevel minLevel = LogLevel.Trace, UnityEngine.Object? context = null)
    {
        _inner = new UnityLogHandler(logHandler, typeof(TCategoryName).Name, minLevel, context);
    }

    /// <summary>
    /// Creates a new instance of <see cref="UnityLogHandler{TCategoryName}"/> using Unity's default log handler.
    /// </summary>
    /// <param name="minLevel">The minimum log level to output. Defaults to <see cref="LogLevel.Trace"/>.</param>
    /// <param name="context">Optional Unity Object context for log messages.</param>
    public UnityLogHandler(LogLevel minLevel = LogLevel.Trace, UnityEngine.Object? context = null)
        : this(Debug.unityLogger.logHandler, minLevel, context)
    {
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _inner.Log(logLevel, eventId, state, exception, formatter);
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return _inner.IsEnabled(logLevel);
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _inner.BeginScope(state);
    }
}
