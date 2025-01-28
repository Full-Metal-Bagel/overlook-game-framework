using UnityEngine;

namespace Game
{
    [CreateAssetMenu(menuName = "KGP/System File/Group Config", fileName = "config")]
    public sealed class SystemGroupConfig : ScriptableObject
    {
        [field: SerializeField] public TickStage TickStage { get; private set; } = TickStage.Update;
        [field: SerializeField] public int TickTimes { get; private set; } = -1;
    }
}
