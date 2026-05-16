using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class AddressablesHelper
    {
        private static readonly Type[] DefaultGroupSchemaTypes =
        {
            typeof(BundledAssetGroupSchema),
            typeof(ContentUpdateGroupSchema),
        };

        public static void EnsureEntry(string assetPath, string address, string groupName = null)
        {
            var settings = Settings();
            if (settings == null) return;

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"[AddressablesHelper] GUID not found for: {assetPath}");
                return;
            }

            var group = EnsureGroup(settings, groupName);
            var existing = settings.FindAssetEntry(guid);
            if (existing != null && existing.address == address && existing.parentGroup == group) return;

            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            AssetDatabase.SaveAssets();
        }

        public static void RemoveEntry(string assetPath)
        {
            var settings = Settings();
            if (settings == null) return;

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) return;

            var existing = settings.FindAssetEntry(guid);
            if (existing == null) return;

            existing.parentGroup.RemoveAssetEntry(existing);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, existing, true);
            AssetDatabase.SaveAssets();
        }

        public static AddressableAssetGroup EnsureGroup(string groupName)
        {
            var settings = Settings();
            return settings == null ? null : EnsureGroup(settings, groupName);
        }

        public static void RemoveGroup(string groupName)
        {
            var settings = Settings();
            if (settings == null || string.IsNullOrEmpty(groupName)) return;

            var group = settings.groups.Find(g => g != null && g.Name == groupName);
            if (group == null) return;

            settings.RemoveGroup(group);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupRemoved, group, true);
            AssetDatabase.SaveAssets();
        }

        private static AddressableAssetGroup EnsureGroup(AddressableAssetSettings settings, string groupName)
        {
            if (string.IsNullOrEmpty(groupName)) return settings.DefaultGroup;

            var existing = settings.groups.Find(g => g != null && g.Name == groupName);
            if (existing != null)
            {
                EnsureGroupSchemas(settings, existing);
                return existing;
            }

            var schemasToCopy = GetSchemasToCopy(settings, null);
            var group = settings.CreateGroup(
                groupName,
                false,
                false,
                true,
                schemasToCopy.Count > 0 ? schemasToCopy : null,
                schemasToCopy.Count == 0 ? DefaultGroupSchemaTypes : Array.Empty<Type>());
            EnsureGroupSchemas(settings, group);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupAdded, group, true);
            AssetDatabase.SaveAssets();
            return group;
        }

        private static bool EnsureGroupSchemas(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            if (settings == null || group == null) return false;

            var changed = false;
            foreach (var schema in GetSchemasToCopy(settings, group))
            {
                if (schema == null || group.GetSchema(schema.GetType()) != null) continue;
                group.AddSchema(schema, false);
                changed = true;
            }

            foreach (var schemaType in DefaultGroupSchemaTypes)
            {
                if (group.GetSchema(schemaType) != null) continue;
                group.AddSchema(schemaType, false);
                changed = true;
            }

            if (!changed) return false;

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupSchemaAdded, group, true);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static List<AddressableAssetGroupSchema> GetSchemasToCopy(AddressableAssetSettings settings, AddressableAssetGroup target)
        {
            var result = new List<AddressableAssetGroupSchema>();
            var source = settings?.DefaultGroup;
            if (source == null || source == target || source.Schemas == null) return result;

            foreach (var schema in source.Schemas)
            {
                if (schema != null) result.Add(schema);
            }

            return result;
        }

        private static AddressableAssetSettings Settings()
        {
            var s = AddressableAssetSettingsDefaultObject.Settings;
            if (s == null) Debug.LogWarning("[AddressablesHelper] AddressableAssetSettings not found.");
            return s;
        }
    }
}
