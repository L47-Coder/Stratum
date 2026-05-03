using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using Stratum.Editor;

internal static class AssetManagerRefresher
{
    private const string ConfigAssetPath = "Assets/Game/Manager/Asset/AssetManagerConfig.asset";
    private static readonly HashSet<string> ExcludedGroupNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Built In Data",
    };

    [EditorSync]
    public static void Run()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<AssetManagerConfig>(ConfigAssetPath);
        if (cfg == null) return;

        ManagerRefreshUtil.Sync(
            (List<AssetManagerData>)cfg.GetConfigList(),
            CollectTargets(),
            static item => item.Key,
            static (key, address) => new AssetManagerData { Key = key, AssetAddress = address });

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
    }

    private static Dictionary<string, string> CollectTargets()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return result;

        foreach (var group in settings.groups)
        {
            if (group == null) continue;
            if (ExcludedGroupNames.Contains(group.Name)) continue;

            foreach (var entry in group.entries)
            {
                if (entry == null) continue;
                if (string.IsNullOrWhiteSpace(entry.address)) continue;
                result[entry.address] = entry.address;
            }
        }
        return result;
    }
}
