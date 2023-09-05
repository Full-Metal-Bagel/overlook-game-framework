#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using OneShot;
using UnityEngine;

namespace Game
{
    public enum TickStage
    {
        Update, PhysicsUpdate
    }

    public readonly ref struct RuntimeSystem
    {
        public IGameSystem System { get; }
        public string Name { get; }
        public TickStage Stage { get; }
        public int RemainedTimes { get; }

        public RuntimeSystem(IGameSystem system, string name, TickStage stage, int remainedTimes)
        {
            System = system;
            Name = name;
            Stage = stage;
            RemainedTimes = remainedTimes;
        }
    }

    [Serializable]
    public class SystemManager
    {
        private readonly IReadOnlyList<SystemGroup> _groups;

        public Container Container { get; }
        public IReadOnlyList<IGameSystem> Systems { get; private set; } = Array.Empty<IGameSystem>();
        public IReadOnlyList<string> SystemNames { get; private set; } = Array.Empty<string>();
        public IReadOnlyList<TickStage> Stages { get; private set; } = Array.Empty<TickStage>();
        public IReadOnlyList<int> RemainedTimes => _remainedTimes;
        public int Count => Systems.Count;

        public RuntimeSystem GetSystem(int index) => new(Systems[index], SystemNames[index], Stages[index], RemainedTimes[index]);

        private int[] _remainedTimes = Array.Empty<int>();

        private readonly ILogHandler _logger;

        public SystemManager(Container container, IReadOnlyList<SystemGroup> groups, ILogHandler<SystemManager> logger)
        {
            _groups = groups;
            Container = container;
            _logger = logger;
            container.RegisterGroupSystems(groups);
        }

        public void CreateSystems()
        {
            var systems = new List<object>();
            var groups = new List<SystemGroup>();
            foreach (var (group, system) in Container.ResolveGroupSystems(_groups))
            {
                _logger.LogInfomation($"create {group.Name}.{system.GetType().Name}");
                systems.Add(system);
                groups.Add(group);
            }

            Systems = systems.Cast<IGameSystem>().ToArray();
            SystemNames = systems.Cast<IGameSystem>().Select(t => t.GetType().Name).ToArray();
            Stages = groups.Select(g => g.TickStage).ToArray();
            _remainedTimes = groups.Select(g => g.TickTimes).ToArray();
        }

        public void Tick(GameData data, TickStage tickStage)
        {
            for (var systemIndex = 0; systemIndex < Systems.Count; systemIndex++)
            {
                var stage = Stages[systemIndex];
                if (stage != tickStage) continue;

                var times = RemainedTimes[systemIndex];
                if (times == 0) continue;
                if (times > 0) _remainedTimes[systemIndex]--;

                try
                {
                    var system = Systems[systemIndex];
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    var systemName = SystemNames[systemIndex];
                    UnityEngine.Profiling.Profiler.BeginSample(systemName);
#endif
                    system.Tick(data);
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
            }
        }
    }
}

