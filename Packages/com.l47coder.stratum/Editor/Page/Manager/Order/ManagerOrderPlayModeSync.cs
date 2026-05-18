using UnityEditor;

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

            ManagerOrderSync.EnsureAndSyncAsset();
        }
    }
}
