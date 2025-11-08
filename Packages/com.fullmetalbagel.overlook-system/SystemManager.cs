#nullable enable

using System;
using System.Collections.Generic;
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

    private int[] _tickStagesBeginIndices = Array.Empty<int>();

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
        using var stageCountsList = new PooledList<int>(32);

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
            for (var i = stageCountsList.Value.Count; i < factory.TickStage; i++)
            {
                stageCountsList.Value.Add(0);
            }
            stageCountsList.Value[factory.TickStage]++;
        }

        var tickStagesBeginIndices = new int[stageCountsList.Value.Count + 1];
        tickStagesBeginIndices[0] = 0;
        for (var i = 0; i < stageCountsList.Value.Count; i++)
        {
            tickStagesBeginIndices[i + 1] = tickStagesBeginIndices[i] + stageCountsList.Value[i];
        }

        var systems = new ISystem[systemData.Value.Count];
        var systemNames = new string[systemData.Value.Count];
        var systemTickStages = new int[systemData.Value.Count];
        var remainedTimes = new int[systemData.Value.Count];

        for (var i = 0; i < systemData.Value.Count; i++)
        {
            var data = systemData.Value[i];
            var index = tickStagesBeginIndices[data.TickStage];
            var length = tickStagesBeginIndices[data.TickStage + 1] - index;
            index += (length - stageCountsList.Value[data.TickStage]);
            stageCountsList.Value[data.TickStage]--;

            systems[index] = data.System;
            systemNames[index] = data.Name;
            systemTickStages[index] = data.TickStage;
            remainedTimes[index] = data.TickTimes;
        }

        // Calculate counts per stage
        var countsPerStage = new Dictionary<int, int>();
        foreach (var stage in systemTickStages)
        {
            countsPerStage[stage] = countsPerStage.GetValueOrDefault(stage) + 1;
        }

        _systems = systems;
        SystemNames = systemNames;
        _systemTickStages = systemTickStages;
        _tickStagesBeginIndices = tickStagesBeginIndices;
        _remainedTimes = remainedTimes;
    }

    public void Tick(int tickStage)
    {
        // Validate tick stage
        if (tickStage < 0 || tickStage >= _tickStagesBeginIndices.Length - 1)
        {
            return; // No systems for this stage
        }

        // Get the range of systems for this tick stage using pre-calculated indices
        var startIndex = _tickStagesBeginIndices[tickStage];
        var endIndex = _tickStagesBeginIndices[tickStage + 1];

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
