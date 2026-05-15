#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Stratum;
using UnityEditor;
using UnityEditor.AddressableAssets;

internal sealed partial class AssetManagerConfig
{
    private static readonly HashSet<string> ExcludedGroupNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Built In Data",
    };

    [EditorSync]
    private void EditorSync()
    {
        var targets = CollectTargets();

        for (var i = DataList.Count - 1; i >= 0; i--)
        {
            var item = DataList[i];
            if (item == null || string.IsNullOrWhiteSpace(item.Key))
            {
                DataList.RemoveAt(i);
                continue;
            }

            var key = item.Key.Trim();
            if (!targets.TryGetValue(key, out var address))
            {
                DataList.RemoveAt(i);
                continue;
            }

            item.AssetAddress = address;
            targets.Remove(key);
        }

        foreach (var kv in targets)
            DataList.Add(new AssetManagerData { Key = kv.Key, AssetAddress = kv.Value });

        EditorUtility.SetDirty(this);
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
#endif
