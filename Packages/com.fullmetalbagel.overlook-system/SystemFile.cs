using System;
using System.Diagnostics.CodeAnalysis;
using OneShot;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Overlook.System;

[CreateAssetMenu(menuName = "KGP/System File/Type", fileName = "000_System")]
public sealed class SystemFile : ScriptableObject, ISystemFile
{
    public string SystemName => _systemType.TypeName;
    [field: SerializeField] public bool Enable { get; private set; } = true;
    [field: SerializeField, MinValue(0), MaxValue(999), Delayed] public int Order { get; private set; } = 0;
    [SerializeField] private GuidTypeReference<IGameSystem> _systemType = default!;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public IGameSystem Resolve(Container container, int systemIndex)
    {
        if (_systemType.Type == null)
        {
            Debug.LogError($"Skip processing null system {name}");
            return EmptySystem.Instance;
        }

        var systemContainer = container.CreateChildContainer();
        systemContainer.Register(_systemType.Type).With(systemIndex).As<IGameSystem>();
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
