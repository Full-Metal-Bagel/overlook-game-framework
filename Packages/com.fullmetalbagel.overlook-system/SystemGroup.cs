#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using OneShot;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using System.Reflection;
#endif

namespace Game
{
    [Serializable]
    public class SystemGroup
    {
        [field: SerializeField] public string Name { get; private set; } = "Update";
        [field: SerializeField] public TickStage TickStage { get; private set; } = TickStage.Update;
        [field: SerializeField] public int TickTimes { get; set; } = -1;
        public IEnumerable<Guid> Systems => _systems.Select(sys => Guid.Parse(sys.Split('|')[0]));
        public int Count => _systems.Length;

#if UNITY_EDITOR
        private static IEnumerable<ValueDropdownItem<string>> _systemTypes = UnityEditor.TypeCache
            .GetTypesDerivedFrom<IGameSystem>()
            .Select(type => (type, attribute: type.GetCustomAttribute<GameSystemAttribute>()))
            .Where(t => t.type is { IsAbstract: false } && t.attribute != null)
            .Select(t => new ValueDropdownItem<string>(t.type.Name, $"{t.attribute.Id}|{t.type.Name}"))
        ;
        [ValueDropdown(nameof(_systemTypes))]
#endif
        [SerializeField, ListDrawerSettings(ShowPaging = false)]
        private string[] _systems = default!;
    }

    public static class SystemGroupExtension
    {
        public static void RegisterGroupSystems(this Container container, IReadOnlyList<SystemGroup> groups)
        {
            var systems =
                from g in groups
                from system in g.Systems
                select SystemsUtils.IdTypeAttributeMap[system].type
            ;

            foreach (var systemType in systems)
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
                from system in @group.Systems
                select (@group, SystemsUtils.IdTypeAttributeMap[system].type)
            ;

            foreach (var (group, systemType) in systemGroupAndTypes)
            {
                yield return (group, container.Resolve(systemType));
            }
        }
    }
}
