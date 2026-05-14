using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class WorkbenchInitializer
    {
        private const string FrameGroupName = "Frame";
        private const string ManagerConfigGroupName = "ManagerConfig";

        private static readonly string ManagerOrderAddress =
            $"{FrameGroupName}/{Path.GetFileNameWithoutExtension(WorkbenchPaths.ManagerOrder)}";

        public static void Ensure()
        {
            try
            {
                EnsureAddressablesInitialized();
                AssetTransporter.Transfer(WorkbenchPaths.GameSkeletonTemplateFolder, WorkbenchPaths.GameRoot);
                AddressablesHelper.EnsureEntry(WorkbenchPaths.ManagerOrder, ManagerOrderAddress, FrameGroupName);
                AddressablesHelper.EnsureGroup(ManagerConfigGroupName);
                AddressablesHelper.EnsureGroup(SoAddressConvention.GroupName);
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
