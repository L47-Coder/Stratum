using System;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class SoAddressablePostprocessor : AssetPostprocessor
    {
        private static readonly string RootPrefix = WorkbenchPaths.SoRoot.TrimEnd('/') + "/";

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            try
            {
                if (importedAssets != null)
                {
                    foreach (var path in importedAssets)
                        TrySyncEntry(path);
                }

                if (movedAssets != null)
                {
                    foreach (var path in movedAssets)
                        TrySyncEntry(path);
                }

                if (deletedAssets != null && deletedAssets.Length > 0)
                {
                    foreach (var path in deletedAssets)
                        if (IsUnderSoRoot(path)) AddressablesHelper.RemoveEntry(path);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SoAddressablePostprocessor] Sync failed: {ex.Message}");
            }
        }

        private static void TrySyncEntry(string assetPath)
        {
            if (!IsUnderSoRoot(assetPath)) return;
            if (!assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) return;

            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (so is not Stratum.BaseSo) return;

            var address = SoAddressConvention.AddressOfAsset(so.GetType(), assetPath);
            if (string.IsNullOrEmpty(address)) return;

            AddressablesHelper.EnsureEntry(assetPath, address, SoAddressConvention.GroupName);
        }

        private static bool IsUnderSoRoot(string assetPath) =>
            !string.IsNullOrEmpty(assetPath) &&
            assetPath.Replace('\\', '/').StartsWith(RootPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
