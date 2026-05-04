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
        private const string ComponentConfigGroupName = "ComponentConfig";
        private const string PrefabGroupName = "Prefab";

        private static readonly string ManagerOrderAddress =
            $"{FrameGroupName}/{Path.GetFileNameWithoutExtension(WorkbenchPaths.ManagerOrder)}";

        private static readonly string ComponentOrderAddress =
            $"{FrameGroupName}/{Path.GetFileNameWithoutExtension(WorkbenchPaths.ComponentOrder)}";

        public static void Ensure()
        {
            try
            {
                EnsureAddressablesInitialized();
                AssetTransporter.Transfer(WorkbenchPaths.GameSkeletonTemplateFolder, WorkbenchPaths.GameRoot);
                AddressablesHelper.EnsureEntry(WorkbenchPaths.ManagerOrder, ManagerOrderAddress, FrameGroupName);
                AddressablesHelper.EnsureEntry(WorkbenchPaths.ComponentOrder, ComponentOrderAddress, FrameGroupName);
                AddressablesHelper.EnsureGroup(ManagerConfigGroupName);
                AddressablesHelper.EnsureGroup(ComponentConfigGroupName);
                AddressablesHelper.EnsureGroup(PrefabGroupName);
                EnsureGroupOrder();
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

        private static void EnsureGroupOrder()
        {
            var config = AssetDatabase.LoadAssetAtPath<AddressableGroupOrderConfig>(WorkbenchPaths.AddressableGroupOrder);
            if (config == null) return;

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            var orderedGroups = new[]
            {
                settings.DefaultGroup,
                settings.groups.Find(g => g != null && g.Name == ManagerConfigGroupName),
                settings.groups.Find(g => g != null && g.Name == ComponentConfigGroupName),
                settings.groups.Find(g => g != null && g.Name == PrefabGroupName),
            };

            var guids = config.GroupGuids;
            var changed = false;
            var insertAt = 0;

            foreach (var group in orderedGroups)
            {
                if (group == null) continue;

                var guid = group.Guid;
                if (guids.Contains(guid)) { insertAt++; continue; }

                guids.Insert(insertAt, guid);
                insertAt++;
                changed = true;
            }

            if (!changed) return;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }
}
