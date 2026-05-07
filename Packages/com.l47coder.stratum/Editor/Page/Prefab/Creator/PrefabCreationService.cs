using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class PrefabCreationService
    {
        public static void Execute(PrefabCreatorState state)
        {
            if (state == null || !state.IsValid) return;

            EnsurePrefabFolder();

            var go = new GameObject(state.InputPrefabName);
            go.AddComponent<Entity>();
            try
            {
                PrefabUtility.SaveAsPrefabAsset(go, state.PrefabFilePath);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }

            AssetDatabase.Refresh();
            AddressablesHelper.EnsureEntry(
                state.PrefabFilePath,
                state.AddressableAddress,
                PrefabCreatorState.AddressableGroupName);

            Debug.Log($"[PrefabCreationService] '{state.InputPrefabName}' created at {state.PrefabFilePath}");
        }

        private static void EnsurePrefabFolder()
        {
            if (AssetDatabase.IsValidFolder(WorkbenchPaths.PrefabRoot)) return;
            AssetDatabase.CreateFolder(WorkbenchPaths.GameRoot, "Prefab");
        }
    }
}
