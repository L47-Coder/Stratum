#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Stratum;
using UnityEditor;

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
#endif
