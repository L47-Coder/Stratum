using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using Stratum.Editor;

internal static class ComponentManagerRefresher
{
    private const string ConfigAssetPath = "Assets/Game/Manager/Component/ComponentManagerConfig.asset";
    private const string TargetGroupName = "ComponentConfig";

    [EditorSync]
    public static void Run()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<ComponentManagerConfig>(ConfigAssetPath);
        if (cfg == null) return;

        var list = (List<ComponentManagerData>)cfg.GetConfigList();
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

            item.ComponentConfigAddress = address;
            targets.Remove(key);
        }

        foreach (var kv in targets)
            list.Add(new ComponentManagerData { Key = kv.Key, ComponentConfigAddress = kv.Value });

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
    }

    private static Dictionary<string, string> CollectTargets()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return result;

        var group = settings.groups.FirstOrDefault(g =>
            g != null && string.Equals(g.Name, TargetGroupName, StringComparison.Ordinal));
        if (group == null) return result;

        foreach (var entry in group.entries)
        {
            if (entry == null) continue;
            if (string.IsNullOrWhiteSpace(entry.address)) continue;
            result[entry.address] = entry.address;
        }
        return result;
    }
}
