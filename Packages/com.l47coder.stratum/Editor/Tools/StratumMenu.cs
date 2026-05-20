using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class StratumMenu
    {
        [MenuItem("Tools/Stratum/Initialize Game Architecture")]
        private static void InitializeGameArchitecture()
        {
            if (GameArchitectureInitializer.Ensure())
            {
                Debug.Log("[Stratum] Game architecture initialized.");
            }
            else
            {
                Debug.LogWarning("[Stratum] Game architecture initialization failed.");
            }
        }

        [MenuItem("Tools/Stratum/Sync Manager Order")]
        private static void SyncManagerOrder()
        {
            var config = ManagerOrderSync.EnsureAndSyncAsset();
            if (config == null)
            {
                Debug.LogWarning("[Stratum] ManagerOrder asset not found.");
                return;
            }

            Debug.Log("[Stratum] Manager order synced.");
        }
    }
}
