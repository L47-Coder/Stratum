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

        [MenuItem("Tools/Stratum/Manager Order")]
        private static void OpenManagerOrder()
        {
            ManagerOrderWindow.Open();
        }
    }
}
