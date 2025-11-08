#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Logging;
using OneShot;
using Overlook.Pool;

namespace Overlook.System;

public readonly record struct RuntimeSystem(ISystem System, string Name, int TickStage, int RemainedTimes);

[Serializable]
public sealed partial class SystemManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly IReadOnlyList<ISystemFactory> _systemFactories;
    public Container Container { get; }
    private ISystem[] _systems = Array.Empty<ISystem>();
    public IReadOnlyList<ISystem> Systems => _systems;
    public IReadOnlyList<string> SystemNames { get; private set; } = Array.Empty<string>();

    /// <summary>
    /// Number of systems per tick stage. Index is the tick stage, value is the count.
    /// Systems are sorted by tick stage, so stage 0 systems come first, then stage 1, etc.
    /// </summary>
    public IReadOnlyList<int> SystemCountsPerStage { get; private set; } = Array.Empty<int>();

    public IReadOnlyList<int> RemainedTimes => _remainedTimes;
    public int Count => Systems.Count;

    private int[] _systemTickStages = Array.Empty<int>();
    private int[] _remainedTimes = Array.Empty<int>();

    public RuntimeSystem GetSystem(int index) => new(Systems[index], SystemNames[index], _systemTickStages[index], RemainedTimes[index]);

    public SystemManager(Container container, IReadOnlyList<ISystemFactory> systemFactories, ILogger<SystemManager> logger)
    {
        Container = container;
        _systemFactories = systemFactories;
        _logger = logger;
    }

    public void CreateSystems()
    {
        using var systemData = new PooledList<(ISystem System, string Name, int TickStage, int TickTimes)>(128);

        // Create all enabled systems
        for (var index = 0; index < _systemFactories.Count; index++)
        {
            var factory = _systemFactories[index];
            if (!factory.Enable)
            {
                LogSystemFactoryInfo(_logger, "Skipping disabled system", factory.SystemName);
                continue;
            }

            LogSystemFactoryInfo(_logger, "Creating system", factory.SystemName);
            var system = factory.Resolve(Container, index);
            systemData.Value.Add((system, factory.SystemName, factory.TickStage, factory.TickTimes));
        }

        // Sort by tick stage
        systemData.Value.Sort((a, b) => a.TickStage.CompareTo(b.TickStage));

        // Extract sorted data
        var systems = new ISystem[systemData.Value.Count];
        var systemNames = new string[systemData.Value.Count];
        var systemTickStages = new int[systemData.Value.Count];
        var remainedTimes = new int[systemData.Value.Count];

        for (var i = 0; i < systemData.Value.Count; i++)
        {
            systems[i] = systemData.Value[i].System;
            systemNames[i] = systemData.Value[i].Name;
            systemTickStages[i] = systemData.Value[i].TickStage;
            remainedTimes[i] = systemData.Value[i].TickTimes;
        }

        // Calculate counts per stage
        var countsPerStage = new Dictionary<int, int>();
        foreach (var stage in systemTickStages)
        {
            countsPerStage[stage] = countsPerStage.GetValueOrDefault(stage) + 1;
        }

        // Convert to array indexed by stage (fill gaps with 0)
        var maxStage = countsPerStage.Count > 0 ? countsPerStage.Keys.Max() : -1;
        var stageCountsArray = new int[maxStage + 1];
        foreach (var (stage, count) in countsPerStage)
        {
            stageCountsArray[stage] = count;
        }

        _systems = systems;
        SystemNames = systemNames;
        _systemTickStages = systemTickStages;
        SystemCountsPerStage = stageCountsArray;
        _remainedTimes = remainedTimes;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "System tick failures should not crash the application")]
    public void Tick(int tickStage)
    {
        // Validate tick stage
        if (tickStage < 0 || tickStage >= SystemCountsPerStage.Count)
        {
            return; // No systems for this stage
        }

        // Calculate the range of systems for this tick stage
        var startIndex = 0;
        for (var i = 0; i < tickStage; i++)
        {
            startIndex += SystemCountsPerStage[i];
        }

        var count = SystemCountsPerStage[tickStage];
        var endIndex = startIndex + count;

        // Tick all systems in this stage
        for (var systemIndex = startIndex; systemIndex < endIndex; systemIndex++)
        {
            ref var times = ref _remainedTimes[systemIndex];
            if (times != 0)
            {
                if (times > 0) times--;
                try
                {
                    var system = Systems[systemIndex];
                    system.Tick();
                }
                catch (Exception ex)
                {
                    LogTickFailed(_logger, ex, SystemNames[systemIndex]);
                }
            }
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Dispose failures should not prevent cleanup of other systems")]
    public void Dispose()
    {
        foreach (var system in _systems)
        {
            if (system is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // Already disposed, ignore
                }
                catch (Exception ex)
                {
                    LogSystemDisposeFailed(_logger, ex, system.GetType().Name);
                }
            }
        }
        _systems = Array.Empty<ISystem>();
    }

    // LoggerMessage delegates for high-performance logging
    [LoggerMessage(Level = LogLevel.Information, Message = "{action}: {systemName}")]
    private static partial void LogSystemFactoryInfo(ILogger logger, string action, string systemName);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Tick failed for system {systemName}")]
    private static partial void LogTickFailed(ILogger logger, Exception ex, string systemName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to dispose system {systemName}")]
    private static partial void LogSystemDisposeFailed(ILogger logger, Exception ex, string systemName);
}
