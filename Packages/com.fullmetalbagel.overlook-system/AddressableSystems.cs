#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using OneShot;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Game;

public static class AddressableSystems
{
    private static readonly AsyncLazy<IList<IResourceLocation>> s_systemLocations = new(LoadSystemLocations);

    private static async UniTask<IList<IResourceLocation>> LoadSystemLocations()
    {
        return await Addressables.LoadResourceLocationsAsync("system").ToUniTask();
    }

    public static async Task<List<RuntimeSystem>> ResolveSystemsAsync(this Container container, string systemsDirectory)
    {
        using var locations = new PooledMemoryList<IResourceLocation>(128);
        var systemLocations = await s_systemLocations;
        for (int index = 0; index < systemLocations.Count; index++) locations.Add(systemLocations[index]);

        for (var i = locations.Count - 1; i >= 0; i--)
        {
            if (!locations[i].InternalId.StartsWith(systemsDirectory, StringComparison.Ordinal)) locations.RemoveAt(i);
        }

        var assets = await locations.LoadAssetsAsync<ScriptableObject>().Task.ConfigureAwait(true);
        Debug.Assert(assets.Count == locations.Count);

        using var configs = new PooledMemoryDictionary<string, SystemGroupConfig>(32);
        for (var i = 0; i < assets.Count; i++)
        {
            if (assets[i] is SystemGroupConfig config)
            {
                configs.Add(Path.GetDirectoryName(locations[i].InternalId)!, config);
            }
        }

        var systems = new List<RuntimeSystem>(64);
        for (var i = 0; i < assets.Count; i++)
        {
            if (assets[i] is not ISystemFile systemFile) continue;
            if (!systemFile.Enable) continue;

            var system = systemFile.Resolve(container, systems.Count);
            var tickStage = TickStage.Update;
            var tickTimes = -1;
            if (configs.TryGetValue(Path.GetDirectoryName(locations[i].InternalId)!, out var config))
            {
                tickStage = config.TickStage;
                tickTimes = config.TickTimes;
            }
            systems.Add(new RuntimeSystem(system, systemFile.SystemName, tickStage, tickTimes));
        }
        return systems;
    }
}
