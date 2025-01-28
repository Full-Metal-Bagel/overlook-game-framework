#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using NodeCanvas.Framework;
using OneShot;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game
{
    [CreateAssetMenu(menuName = "KGP/System File/Graph", fileName = "000_System")]
    public sealed class GraphSystemFile : ScriptableObject, ISystemFile
    {
        public string SystemName =>
#if UNITY_EDITOR
            _graphAsset.editorAsset == null ? "" : _graphAsset.editorAsset.name;
#else
            name[4..];
#endif

        [field: SerializeField] public bool Enable { get; private set; } = true;
        [field: SerializeField, MinValue(0), MaxValue(999), Delayed] public int Order { get; private set; } = 0;
        [SerializeField] private AssetReferenceT<Graph> _graphAsset = default!;
        [SerializeField] private bool _isShared = false;

        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        public IGameSystem Resolve(Container container, int systemIndex)
        {
            var graph = _graphAsset.WaitForAsset();
            if (graph == null)
            {
                Debug.LogError($"Skip processing null graph system {name}");
                return EmptySystem.Instance;
            }

            var systemContainer = container.CreateChildContainer();
            systemContainer.Register(_isShared ? typeof(SharedFlowScriptEntitySystem) : typeof(InstancedFlowScriptEntitySystem))
                .With(systemIndex, graph)
                .As<IGameSystem>();

            try
            {
                return systemContainer.Resolve<IGameSystem>();
            }
            catch (Exception e)
            {
                Debug.LogError($"Skip adding system {name} to {nameof(SystemManager)}" +
                               " because an exception was thrown during its initialization\n" + e);
                Debug.LogException(e);
                return EmptySystem.Instance;
            }
        }
    }
}
