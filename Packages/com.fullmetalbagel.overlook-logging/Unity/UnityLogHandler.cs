using System;
using UnityEngine;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// An implementation of <see cref="ILogger"/> that wraps Unity's <see cref="ILogHandler"/>.
/// </summary>
public sealed class UnityLogHandler : ILogger
{
    private readonly ILogHandler _logHandler;
    private readonly string _categoryName;
    private readonly LogLevel _minLevel;
    private readonly UnityEngine.Object? _context;

    /// <summary>
    /// Creates a new instance of <see cref="UnityLogHandler"/>.
    /// </summary>
    /// <param name="logHandler">The Unity log handler to wrap.</param>
    /// <param name="categoryName">The category name for this logger.</param>
    /// <param name="minLevel">The minimum log level to output. Defaults to <see cref="LogLevel.Trace"/>.</param>
    /// <param name="context">Optional Unity Object context for log messages.</param>
    public UnityLogHandler(ILogHandler logHandler, string categoryName = "", LogLevel minLevel = LogLevel.Trace, UnityEngine.Object? context = null)
    {
        _logHandler = logHandler ?? throw new ArgumentNullException(nameof(logHandler));
        _categoryName = categoryName;
        _minLevel = minLevel;
        _context = context;
    }

    /// <summary>
    /// Creates a new instance of <see cref="UnityLogHandler"/> using Unity's default log handler.
    /// </summary>
    /// <param name="categoryName">The category name for this logger.</param>
    /// <param name="minLevel">The minimum log level to output. Defaults to <see cref="LogLevel.Trace"/>.</param>
    /// <param name="context">Optional Unity Object context for log messages.</param>
    public UnityLogHandler(string categoryName = "", LogLevel minLevel = LogLevel.Trace, UnityEngine.Object? context = null)
        : this(Debug.unityLogger.logHandler, categoryName, minLevel, context)
    {
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        if (formatter == null)
            throw new ArgumentNullException(nameof(formatter));

        var message = formatter(state, exception);

        if (string.IsNullOrEmpty(message) && exception == null)
            return;

        var logType = MapLogLevel(logLevel);
        var formattedMessage = FormatMessage(logLevel, eventId, message);

        if (exception != null)
        {
            _logHandler.LogException(exception, _context);
            if (!string.IsNullOrEmpty(message))
            {
                _logHandler.LogFormat(logType, _context, "{0}", formattedMessage);
            }
        }
        else
        {
            _logHandler.LogFormat(logType, _context, "{0}", formattedMessage);
        }
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && logLevel >= _minLevel;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return NullScope.Instance;
    }

    private static LogType MapLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => LogType.Log,
            LogLevel.Debug => LogType.Log,
            LogLevel.Information => LogType.Log,
            LogLevel.Warning => LogType.Warning,
            LogLevel.Error => LogType.Error,
            LogLevel.Critical => LogType.Error,
            _ => LogType.Log
        };
    }

    private string FormatMessage(LogLevel logLevel, EventId eventId, string message)
    {
        var levelPrefix = logLevel switch
        {
            LogLevel.Trace => "[TRACE]",
            LogLevel.Debug => "[DEBUG]",
            LogLevel.Information => "[INFO]",
            LogLevel.Warning => "[WARN]",
            LogLevel.Error => "[ERROR]",
            LogLevel.Critical => "[CRIT]",
            _ => ""
        };

        if (!string.IsNullOrEmpty(_categoryName))
        {
            if (eventId.Id != 0)
                return $"{levelPrefix} [{_categoryName}] [{eventId}] {message}";
            return $"{levelPrefix} [{_categoryName}] {message}";
        }

        if (eventId.Id != 0)
            return $"{levelPrefix} [{eventId}] {message}";
        return $"{levelPrefix} {message}";
    }

    /// <summary>
    /// A scope that does nothing.
    /// </summary>
    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        private NullScope()
        {
        }

        public void Dispose()
        {
        }
    }
}
