using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using Stratum.Editor;

internal static class PrefabManagerRefresher
{
    private const string ConfigAssetPath = "Assets/Game/Manager/Prefab/PrefabManagerConfig.asset";
    private const string PrefabGroupName = "Prefab";
    private const string PrefabAddressPrefix = "Prefab/";

    [EditorSync]
    public static void Run()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<PrefabManagerConfig>(ConfigAssetPath);
        if (cfg == null) return;

        ManagerRefreshUtil.Sync(
            (List<PrefabManagerData>)cfg.GetConfigList(),
            CollectTargets(),
            static item => item.Key,
            static (key, address) => new PrefabManagerData { Key = key, PrefabAddress = address });

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
    }

    private static Dictionary<string, string> CollectTargets()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return result;

        var group = settings.groups.FirstOrDefault(g =>
            g != null && string.Equals(g.Name, PrefabGroupName, StringComparison.OrdinalIgnoreCase));
        if (group == null) return result;

        foreach (var entry in group.entries)
        {
            if (entry == null) continue;
            var address = entry.address;
            if (string.IsNullOrWhiteSpace(address)) continue;

            var key = address.StartsWith(PrefabAddressPrefix, StringComparison.Ordinal)
                ? address[PrefabAddressPrefix.Length..]
                : address;
            if (string.IsNullOrWhiteSpace(key)) continue;

            result[key] = address;
        }
        return result;
    }
}
