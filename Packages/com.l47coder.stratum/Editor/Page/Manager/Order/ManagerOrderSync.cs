using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Stratum.Editor
{
    internal static class ManagerOrderSync
    {
        public static ManagerOrderConfig EnsureAndSyncAsset()
        {
            WorkbenchInitializer.Ensure();
            return SyncAsset();
        }

        public static ManagerOrderConfig SyncAsset()
        {
            var config = AssetDatabase.LoadAssetAtPath<ManagerOrderConfig>(WorkbenchPaths.ManagerOrder);
            if (config == null) return null;

            Sync(config);
            return config;
        }

        public static bool Sync(ManagerOrderConfig config)
        {
            if (config == null) return false;

            var liveTypes = TypeCache.GetTypesDerivedFrom<IManager>()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .ToDictionary(t => t.Name, t => t, StringComparer.Ordinal);

            var removed = config.Entries.RemoveAll(e => !liveTypes.ContainsKey(e.Name)) > 0;

            var existing = new HashSet<string>(config.Entries.Select(e => e.Name), StringComparer.Ordinal);
            var added = false;
            foreach (var kv in liveTypes.Where(kv => !existing.Contains(kv.Key)).OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                config.Entries.Add(new ManagerOrderEntry
                {
                    Name = kv.Key,
                    AssemblyQualifiedName = kv.Value.AssemblyQualifiedName,
                });
                added = true;
            }

            var refreshed = false;
            foreach (var entry in config.Entries)
            {
                if (!liveTypes.TryGetValue(entry.Name, out var t)) continue;
                if (string.Equals(entry.AssemblyQualifiedName, t.AssemblyQualifiedName, StringComparison.Ordinal)) continue;
                entry.AssemblyQualifiedName = t.AssemblyQualifiedName;
                refreshed = true;
            }

            if (!removed && !added && !refreshed) return false;

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return true;
        }
    }
}
