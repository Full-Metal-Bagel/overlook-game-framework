using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Overlook.System;

[CreateAssetMenu(menuName = "KGP/Systems", fileName = "Systems")]
public class SystemsConfig : ScriptableObject
{
    public IReadOnlyList<SystemGroup> Systems => _systems;

    [SerializeField, ListDrawerSettings(ShowPaging = false, ShowIndexLabels = true, DefaultExpandedState = true, ShowItemCount = true)]
    private SystemGroup[] _systems = default!;

#if UNITY_EDITOR
        [ShowInInspector, HideLabel, PropertyOrder(-1)]
        private string _searchQuery = "";

        [ShowInInspector, HorizontalGroup("Search"), PropertyOrder(-1)]
        private string _groupIndex = "";

        [ShowInInspector, HorizontalGroup("Search"), PropertyOrder(-1)]
        private string _systemIndex = "";

        [Button("Search"), PropertyOrder(-1)]
        private void SearchAndHighlight()
        {
            if (string.IsNullOrWhiteSpace(_searchQuery))
            {
                Debug.LogWarning("Search query is empty.");
                return;
            }

            _groupIndex = string.Empty;
            _systemIndex = string.Empty;
            for (int groupIndex = 0; groupIndex < _systems.Length; groupIndex++)
            {
                var systemGroup = _systems[groupIndex];
                for (int systemIndex = 0; systemIndex < systemGroup.Systems.Count; systemIndex++)
                {
                    var systemData = systemGroup.Systems[systemIndex];
                    if (systemData != null && systemData.Type.TypeName.Contains(_searchQuery, System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(_groupIndex))
                        {
                            _groupIndex += ",";
                            _systemIndex += ",";
                        }

                        _groupIndex += groupIndex;
                        _systemIndex += systemIndex;
                        Debug.Log($"Found and highlighted SystemData: {systemData.Type.TypeName} in Group {groupIndex}, Index {systemIndex}");
                    }
                }
            }

            if (!string.IsNullOrEmpty(_groupIndex))
            {
                return;
            }

            Debug.LogWarning($"No matching SystemData found for query: {_searchQuery}");
        }
#endif
}
