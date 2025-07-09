using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OneShot;
using UnityEngine;

namespace Overlook.System;

[Serializable]
public sealed class AddressableSystemManager
{
    private readonly List<RuntimeSystem> _systems = new();
    public Container Container { get; }
    public int Count => _systems.Count;

    public RuntimeSystem GetSystem(int index) => _systems[index];
    public IReadOnlyList<RuntimeSystem> Systems => _systems;

    private readonly string _directory;
    private readonly ILogHandler _logger;

    public AddressableSystemManager(Container container, string directory, ILogHandler<SystemManager> logger)
    {
        Container = container;
        _directory = directory;
        _logger = logger;
    }

    public async Task CreateSystems()
    {
        var systems = await Container.ResolveSystemsAsync(_directory).ConfigureAwait(true);
        _systems.Clear();
        _systems.AddRange(systems);
    }

    public void Tick(GameData data, TickStage tickStage)
    {
        for (var systemIndex = 0; systemIndex < Count; systemIndex++)
        {
            var system = _systems[systemIndex];
            if (system.Stage == tickStage && system.RemainedTimes != 0)
            {
                try
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    UnityEngine.Profiling.Profiler.BeginSample(system.Name);
#endif
                    system.System.Tick(data);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    UnityEngine.Profiling.Profiler.EndSample();
#endif
                }
#pragma warning disable CA1031
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    _logger.LogException(ex);
                }
                finally
                {
                    if (system.RemainedTimes > 0)
                        _systems[systemIndex] = system with { RemainedTimes = system.RemainedTimes - 1 };
                }
            }
            if (tickStage == TickStage.Update) data.TickSystemEvents(systemIndex);
        }
    }
}
