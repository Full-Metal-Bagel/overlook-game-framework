#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using OneShot;
using UnityEngine;

namespace Game
{
    [Serializable]
    public class SystemGroup
    {
        [field: SerializeField] public string Name { get; private set; } = "Update";
        [field: SerializeField] public TickStage TickStage { get; private set; } = TickStage.Update;
        [field: SerializeField] public int TickTimes { get; set; } = -1;
        public IEnumerable<Guid> SystemsGuid => _systems.Select(system => system.Guid);
        public IEnumerable<Type> SystemsType => _systems.Select(system => system.Type);
        public int Count => _systems.Length;
        [SerializeField] private GuidTypeReference<IGameSystem>[] _systems = default!;
    }

    public static class SystemGroupExtension
    {
        public static void RegisterGroupSystems(this Container container, IReadOnlyList<SystemGroup> groups)
        {
            foreach (var systemType in groups.SelectMany(g => g.SystemsType))
            {
                if (!container.IsRegisteredInHierarchy(systemType))
                {
                    container.Register(systemType).Singleton().AsInterfaces().AsSelf();
                }
            }
        }

        [Pure, MustUseReturnValue]
        public static IEnumerable<(SystemGroup group, object system)> ResolveGroupSystems(this Container container, IReadOnlyList<SystemGroup> groups)
        {
            var systemGroupAndTypes =
                from @group in groups
                from type in @group.SystemsType
                select (@group, type)
            ;

            foreach (var (group, systemType) in systemGroupAndTypes)
            {
                yield return (group, container.Resolve(systemType));
            }
        }
    }
}
