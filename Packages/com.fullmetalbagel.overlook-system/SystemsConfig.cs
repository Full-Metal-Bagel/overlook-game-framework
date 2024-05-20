#nullable enable

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(menuName = "KGP/Systems", fileName = "Systems")]
    public class SystemsConfig : ScriptableObject
    {
        public IReadOnlyList<SystemGroup> Systems => _systems;
        [SerializeField, ListDrawerSettings(ShowPaging = false)] private SystemGroup[] _systems = default!;
    }
}
