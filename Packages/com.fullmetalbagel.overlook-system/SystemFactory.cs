#nullable enable

using System;
using Microsoft.Extensions.Logging;
using OneShot;

namespace Overlook.System;

/// <summary>
/// Factory for creating systems from CSV row data.
/// CSV format: enabled, system name, system type (type|guid), tick stage, tick times
/// Example: +, Foo, Game.FooSystem|XXXX-XXXX-XXXX, 0, -1
/// Note: tick times of -1 means infinite ticks, 0 means disabled, positive values count down
/// </summary>
public sealed partial class CsvFileSystemFactory : ISystemFactory
{
    private readonly ILogger<CsvFileSystemFactory> _logger;
    private readonly Type? _systemType;

    public string SystemName { get; }
    public bool Enable { get; }
    public byte TickStage { get; }
    public int TickTimes { get; }

    public CsvFileSystemFactory(string row, ILogger<CsvFileSystemFactory> logger)
    {
        _logger = logger;

        // Parse CSV row: enabled, system name, system type (type|guid), tick stage, tick times
        var parts = row.Split(',');
        if (parts.Length < 5)
        {
            LogInvalidCsvFormat(_logger, row);
            SystemName = "Invalid";
            Enable = false;
            TickStage = 0;
            TickTimes = -1;
            _systemType = null;
            return;
        }

        // Parse enabled flag (+/-)
        var enabledStr = parts[0].Trim();
        Enable = enabledStr == "+";

        // Parse system name
        SystemName = parts[1].Trim();

        // Parse system type (either full type name or GUID)
        var typeStr = parts[2].Trim();
        _systemType = ResolveSystemType(typeStr);

        // Parse tick stage
        var tickStageStr = parts[3].Trim();
        if (!byte.TryParse(tickStageStr, out var tickStage))
        {
            LogInvalidTickStage(_logger, tickStageStr, SystemName);
            TickStage = 0; // Default to 0 if parsing fails
        }
        else
        {
            TickStage = tickStage;
        }

        // Parse tick times
        var tickTimesStr = parts[4].Trim();
        if (!int.TryParse(tickTimesStr, out var tickTimes))
        {
            LogInvalidTickTimes(_logger, tickTimesStr, SystemName);
            TickTimes = -1; // Default to infinite if parsing fails
        }
        else
        {
            TickTimes = tickTimes;
        }

        if (_systemType == null)
        {
            LogUnresolvedSystemType(_logger, typeStr, SystemName);
        }
    }

    private Type? ResolveSystemType(string typeIdentifier)
    {
        // Format: [FullTypeName]|[GUID]
        var parts = typeIdentifier.Split('|');

        // Try to resolve by GUID first (more reliable)
        if (parts.Length > 1 && Guid.TryParse(parts[1].Trim(), out var guid))
        {
            if (SystemsUtils.IdTypeMap.TryGetValue(guid, out var type))
            {
                return type;
            }
            LogNoSystemTypeForGuid(_logger, guid);
        }

        // Fall back to resolving by full type name
        var typeName = parts[0].Trim();
        var resolvedType = Type.GetType(typeName);
        if (resolvedType != null && typeof(ISystem).IsAssignableFrom(resolvedType))
        {
            return resolvedType;
        }

        LogCouldNotResolveType(_logger, typeIdentifier);
        return null;
    }

    public ISystem Resolve(Container container, int systemIndex)
    {
        if (_systemType == null)
        {
            LogSkipProcessingNullSystem(_logger, SystemName);
            return EmptySystem.Instance;
        }

        var systemContainer = container.CreateChildContainer();
        systemContainer.Register(_systemType).With(systemIndex).As<ISystem>();
        try
        {
            return systemContainer.Resolve<ISystem>();
        }
        catch (MissingMethodException e)
        {
            LogMissingMethodException(_logger, e, SystemName);
            return EmptySystem.Instance;
        }
        catch (InvalidOperationException e)
        {
            LogInvalidDependencyConfiguration(_logger, e, SystemName);
            return EmptySystem.Instance;
        }
#pragma warning disable CA1031 // Do not catch general exception types - Required to prevent system initialization failures from crashing the application
        catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            LogUnexpectedException(_logger, e, SystemName);
            return EmptySystem.Instance;
        }
    }

    // LoggerMessage delegates for high-performance logging
    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid CSV row format: {row}. Expected format: enabled, system name, system type, tick stage, tick times")]
    private static partial void LogInvalidCsvFormat(ILogger logger, string row);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid tick stage '{tickStageStr}' for system '{systemName}', defaulting to 0")]
    private static partial void LogInvalidTickStage(ILogger logger, string tickStageStr, string systemName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid tick times '{tickTimesStr}' for system '{systemName}', defaulting to -1 (infinite)")]
    private static partial void LogInvalidTickTimes(ILogger logger, string tickTimesStr, string systemName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not resolve system type '{typeStr}' for system '{systemName}'")]
    private static partial void LogUnresolvedSystemType(ILogger logger, string typeStr, string systemName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No system type found for GUID: {guid}")]
    private static partial void LogNoSystemTypeForGuid(ILogger logger, Guid guid);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not resolve type: {typeIdentifier}")]
    private static partial void LogCouldNotResolveType(ILogger logger, string typeIdentifier);

    [LoggerMessage(Level = LogLevel.Error, Message = "Skip processing null system {systemName}")]
    private static partial void LogSkipProcessingNullSystem(ILogger logger, string systemName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Skip adding system {systemName} because a required constructor or method was not found")]
    private static partial void LogMissingMethodException(ILogger logger, Exception exception, string systemName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Skip adding system {systemName} because of invalid dependency injection configuration")]
    private static partial void LogInvalidDependencyConfiguration(ILogger logger, Exception exception, string systemName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Skip adding system {systemName} because an unexpected exception was thrown during its initialization")]
    private static partial void LogUnexpectedException(ILogger logger, Exception exception, string systemName);
}
