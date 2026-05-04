using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class AddressablesHelper
    {
        public static void EnsureEntry(string assetPath, string address, string groupName = null)
        {
            var settings = Settings();
            if (settings == null) return;

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"[AddressablesHelper] GUID not found for: {assetPath}");
                return;
            }

            var group   = EnsureGroup(settings, groupName);
            var existing = settings.FindAssetEntry(guid);
            if (existing != null && existing.address == address && existing.parentGroup == group) return;

            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            AssetDatabase.SaveAssets();
        }

        public static void RemoveEntry(string assetPath)
        {
            var settings = Settings();
            if (settings == null) return;

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) return;

            var existing = settings.FindAssetEntry(guid);
            if (existing == null) return;

            existing.parentGroup.RemoveAssetEntry(existing);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, existing, true);
            AssetDatabase.SaveAssets();
        }

        public static AddressableAssetGroup EnsureGroup(string groupName)
        {
            var settings = Settings();
            return settings == null ? null : EnsureGroup(settings, groupName);
        }

        public static void RemoveGroup(string groupName)
        {
            var settings = Settings();
            if (settings == null || string.IsNullOrEmpty(groupName)) return;

            var group = settings.groups.Find(g => g != null && g.Name == groupName);
            if (group == null) return;

            settings.RemoveGroup(group);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupRemoved, group, true);
            AssetDatabase.SaveAssets();
        }

        private static AddressableAssetGroup EnsureGroup(AddressableAssetSettings settings, string groupName)
        {
            if (string.IsNullOrEmpty(groupName)) return settings.DefaultGroup;
            return settings.groups.Find(g => g != null && g.Name == groupName)
                ?? settings.CreateGroup(groupName, false, false, true, null);
        }

        private static AddressableAssetSettings Settings()
        {
            var s = AddressableAssetSettingsDefaultObject.Settings;
            if (s == null) Debug.LogWarning("[AddressablesHelper] AddressableAssetSettings not found.");
            return s;
        }
    }
}
