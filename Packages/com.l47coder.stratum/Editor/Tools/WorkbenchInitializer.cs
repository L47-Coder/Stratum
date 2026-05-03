using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class WorkbenchInitializer
    {
        public static void Ensure()
        {
            try
            {
                EnsureAddressablesInitialized();
                AssetTransporter.Transfer(WorkbenchPaths.GameSkeletonTemplateFolder, WorkbenchPaths.GameRoot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorkbenchInitializer] Ensure failed: {ex}");
            }
        }

        private static void EnsureAddressablesInitialized()
        {
            if (AddressableAssetSettingsDefaultObject.Settings != null) return;
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null) { Debug.LogError("[WorkbenchInitializer] Failed to create AddressableAssetSettings."); return; }
            AssetDatabase.SaveAssets();
        }
    }
}
