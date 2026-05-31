using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    [InitializeOnLoad]
    internal static class ManagerOrderPlayModeSync
    {
        static ManagerOrderPlayModeSync()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;

            try
            {
                ManagerOrderSync.EnsureAndSyncAsset();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.isPlaying = false;
            }
        }
    }
}
