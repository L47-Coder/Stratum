using System;
using System.Collections.Generic;
using System.Linq;
using Stratum.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;

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

        var list = (List<PrefabManagerData>)cfg.GetConfigList();
        var targets = CollectTargets();

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

            item.PrefabAddress = address;
            targets.Remove(key);
        }

        foreach (var kv in targets)
            list.Add(new PrefabManagerData { Key = kv.Key, PrefabAddress = kv.Value });

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
