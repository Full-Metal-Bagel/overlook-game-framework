#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using NodeCanvas.Framework;
using OneShot;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game
{
    [Serializable]
    public class SystemGroup
    {
        [field: SerializeField, HorizontalGroup, HideLabel] public string Name { get; private set; } = "Update";
        [field: SerializeField, HorizontalGroup, HideLabel] public TickStage TickStage { get; private set; } = TickStage.Update;
        [field: SerializeField, HorizontalGroup, HideLabel] public int TickTimes { get; set; } = -1;
        public IReadOnlyList<SystemData> Systems => _systems;
        public int Count => _systems.Length;

        [SerializeField, ListDrawerSettings(DefaultExpandedState = true)]
        private SystemData[] _systems = default!;

        [Serializable]
        [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
        public class SystemData
        {
            [field: SerializeField, HideLabel, HorizontalGroup, TypeConstraint(BaseType = typeof(IGameSystem))]
            public GuidTypeReference Type { get; private set; } = default!;

            [field: SerializeField, HideLabel, HorizontalGroup, ShowIf(nameof(IsGraphSystem)), AssetReferenceUILabelRestriction("system")]
            public GameAssetReference<Graph> Graph { get; private set; } = default!;

            private bool IsGraphSystem => Type is { Type: not null } && Type.Type.GetCustomAttribute<GraphSystemAttribute>() != null;

            public void Deconstruct(out GuidTypeReference type, out AssetReferenceT<Graph> graph)
            {
                type = Type;
                graph = Graph;
            }
        }
    }

    public static class SystemGroupExtension
    {
        public static void RegisterGroupSystems(this Container container, IReadOnlyList<SystemGroup> groups)
        {
            foreach (var (group, system, graph) in from g in groups
                                                   from t in g.Systems
                                                   select (g, t.Type, t.Graph)
            )
            {
                var systemType = system.Type;
                if (systemType == null)
                {
                    Debug.LogError($"invalid system: {group.Name}.{system.IdAndName}");
                    continue;
                }

                if (!container.IsRegisteredInHierarchy(systemType))
                {
                    container.Register(systemType).Singleton().AsInterfaces().AsSelf();
                }
            }
        }

        [Pure, MustUseReturnValue]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        public static IEnumerable<(SystemGroup group, object system)> ResolveGroupSystems(this Container container, IReadOnlyList<SystemGroup> groups)
        {
            var systemGroupAndTypes =
                from @group in groups
                from t in @group.Systems
                select (@group, t.Type.Type, t.Graph)
            ;

            foreach (var (group, systemType, graph) in systemGroupAndTypes)
            {
                if (systemType == null)
                {
                    Debug.LogError($"Skip processing null system in group {group.Name}");
                    continue;
                }

                Debug.Assert(systemType.GetCustomAttribute<GraphSystemAttribute>() == null || graph != null);
                var systemContainer = container.CreateChildContainer();
                systemContainer.Register(systemType).With(graph).AsSelf();
                (SystemGroup group, object system) ret;
                try
                {
                    ret = (group, systemContainer.Resolve(systemType));
                }
                catch (Exception e)
                {
                    var graphName = "";
#if UNITY_EDITOR
                    graphName = graph?.editorAsset?.name;
                    graphName = string.IsNullOrEmpty(graphName) ? "" : $" ({graphName})";
#endif
                    Debug.LogError($"Skip adding system {group.Name}.{systemType.Name}{graphName} to {nameof(SystemManager)}" +
                                   " because an exception was thrown during its initialization\n" + e);
                    Debug.LogException(e);
                    continue;
                }
                yield return ret;
            }
        }
    }
}
