using System;
using System.Collections.Generic;
using Stratum;
using UnityEditor;
using UnityEditor.AddressableAssets;

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

        var targets = CollectTargets();
        var list = cfg.DataList;

        for (var i = list.Count - 1; i >= 0; i--)
        {
            var item = list[i];
            if (item == null || string.IsNullOrWhiteSpace(item.Key))
            {
                list.RemoveAt(i);
                continue;
            }

            var key = item.Key.Trim();
            if (!targets.TryGetValue(key, out var address))
            {
                list.RemoveAt(i);
                continue;
            }

            item.AssetAddress = address;
            targets.Remove(key);
        }

        foreach (var kv in targets)
            list.Add(new AssetManagerData { Key = kv.Key, AssetAddress = kv.Value });

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
