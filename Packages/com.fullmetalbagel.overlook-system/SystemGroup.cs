#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using OneShot;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game
{
    [Serializable]
    public class SystemGroup
    {
        [field: SerializeField, HorizontalGroup, HideLabel] public string Name { get; private set; } = "Update";
        [field: SerializeField, HorizontalGroup, HideLabel] public TickStage TickStage { get; private set; } = TickStage.Update;
        [field: SerializeField, HorizontalGroup, HideLabel] public int TickTimes { get; set; } = -1;
        public IEnumerable<Guid> SystemsGuid => _systems.Select(system => system.Guid);
        public IEnumerable<Type?> SystemsType => _systems.Select(system => system.Type);
        public int Count => _systems.Length;
        [SerializeField, TypeConstraint(BaseType = typeof(IGameSystem))]
        internal GuidTypeReference[] _systems = default!;
    }

    public static class SystemGroupExtension
    {
        public static void RegisterGroupSystems(this Container container, IReadOnlyList<SystemGroup> groups)
        {
            foreach (var system in groups.SelectMany(g => g._systems))
            {
                var systemType = system.Type;
                if (systemType == null)
                {
                    Debug.LogError($"invalid system: {system.GuidAndName}");
                    continue;
                }

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
