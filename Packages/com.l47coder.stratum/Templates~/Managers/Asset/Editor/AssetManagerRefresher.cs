using System;
using System.Collections;
using System.Collections.Generic;
using Stratum;
using UnityEditor;

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
        foreach (var group in EnumerateProperty(GetAddressableSettings(), "groups"))
        {
            if (group == null) continue;
            var groupName = GetStringProperty(group, "Name");
            if (ExcludedGroupNames.Contains(groupName)) continue;

            foreach (var entry in EnumerateProperty(group, "entries"))
            {
                if (entry == null) continue;
                var address = GetStringProperty(entry, "address");
                if (string.IsNullOrWhiteSpace(address)) continue;
                result[address] = address;
            }
        }
        return result;
    }

    private static object GetAddressableSettings()
    {
        var type = Type.GetType("UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject, Unity.Addressables.Editor");
        return type?.GetProperty("Settings")?.GetValue(null);
    }

    private static IEnumerable EnumerateProperty(object target, string propertyName)
    {
        if (target == null) yield break;
        if (target.GetType().GetProperty(propertyName)?.GetValue(target) is not IEnumerable values) yield break;

        foreach (var value in values)
            yield return value;
    }

    private static string GetStringProperty(object target, string propertyName) =>
        target?.GetType().GetProperty(propertyName)?.GetValue(target) as string;
}
