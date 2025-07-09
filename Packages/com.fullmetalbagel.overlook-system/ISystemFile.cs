#nullable enable
using OneShot;

#if UNITY_EDITOR
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
#endif

namespace Game;

public interface ISystemFile
{
    int Order { get; }
    string SystemName { get; }
    bool Enable { get; }

    IGameSystem Resolve(Container container, int systemIndex);
}

#if UNITY_EDITOR
public class SystemFilePostProcessor : UnityEditor.AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets)
        {
            // TODO: editor settings
            if (!path.EndsWith(".asset")) continue;
            if (!path.StartsWith("Assets/_ProjectN/Systems")) continue;

            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.ScriptableObject>(path);
            var systemFile = asset as ISystemFile;
            if (systemFile == null) continue;

            var expectedName = $"{systemFile.Order:D3} {systemFile.SystemName}";
            if (asset.name != expectedName)
            {
                asset.name = expectedName;
                UnityEditor.AssetDatabase.RenameAsset(path, expectedName);
            }

            // Handle Deprecated label
            const string deprecated = "Deprecated";
            var labels = UnityEditor.AssetDatabase.GetLabels(asset);
            var hasDeprecatedLabel = System.Array.IndexOf(labels, deprecated) != -1;
            if (!systemFile.Enable && !hasDeprecatedLabel)
            {
                var newLabels = new string[labels.Length + 1];
                labels.CopyTo(newLabels, 0);
                newLabels[labels.Length] = deprecated;
                UnityEditor.AssetDatabase.SetLabels(asset, newLabels);
            }
            else if (systemFile.Enable && hasDeprecatedLabel)
            {
                var newLabels = labels.Where(l => l != deprecated).ToArray();
                UnityEditor.AssetDatabase.SetLabels(asset, newLabels);
            }
        }
    }
}
#endif
