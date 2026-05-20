using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class GameArchitectureInitializer
    {
        private const string AppGroupName = "App";

        private static readonly string ManagerOrderAddress =
            $"{AppGroupName}/{Path.GetFileNameWithoutExtension(StratumPaths.ManagerOrder)}";

        public static bool Ensure()
        {
            try
            {
                if (!EnsureAddressablesInitialized()) return false;
                AssetTransporter.Transfer(StratumPaths.GameSkeletonTemplateFolder, StratumPaths.GameRoot);
                AddressablesHelper.EnsureEntry(StratumPaths.ManagerOrder, ManagerOrderAddress, AppGroupName);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameArchitectureInitializer] Ensure failed: {ex}");
                return false;
            }
        }

        private static bool EnsureAddressablesInitialized()
        {
            if (AddressableAssetSettingsDefaultObject.Settings != null) return true;
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("[GameArchitectureInitializer] Failed to create AddressableAssetSettings.");
                return false;
            }

            AssetDatabase.SaveAssets();
            return true;
        }
    }
}
