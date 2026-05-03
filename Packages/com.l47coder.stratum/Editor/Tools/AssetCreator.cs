using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class AssetCreator
    {
        public static bool Ensure<T>(string assetPath, string address = null) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(assetPath) != null) return false;

            EnsureFolder(assetPath[..assetPath.LastIndexOf('/')]);

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();

            if (address != null) RegisterAddressable(assetPath, address);
            return true;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            var slash = folderPath.LastIndexOf('/');
            var parent = folderPath[..slash];
            var leaf = folderPath[(slash + 1)..];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void RegisterAddressable(string assetPath, string address)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogWarning($"[AssetCreator] AddressableAssetSettings not found; skipping: {assetPath}"); return; }

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) return;

            var existing = settings.FindAssetEntry(guid);
            if (existing != null && existing.address == address) return;

            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            AssetDatabase.SaveAssets();
        }
    }
}
