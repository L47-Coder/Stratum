using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Stratum.Editor
{
    internal static class ManagerOrderSync
    {
        public static void Sync(ManagerOrderConfig config)
        {
            if (config == null) return;

            var liveTypes = TypeCache.GetTypesDerivedFrom<IManager>()
                .Where(t => !t.IsAbstract)
                .ToDictionary(t => t.Name, t => t, StringComparer.Ordinal);

            var removed = config.Entries.RemoveAll(e => !liveTypes.ContainsKey(e.Name)) > 0;

            var existing = new HashSet<string>(config.Entries.Select(e => e.Name), StringComparer.Ordinal);
            var added = false;
            foreach (var kv in liveTypes.Where(kv => !existing.Contains(kv.Key)))
            {
                config.Entries.Add(new ManagerOrderEntry
                {
                    Name = kv.Key,
                    AssemblyQualifiedName = kv.Value.AssemblyQualifiedName,
                });
                added = true;
            }

            var backfilled = false;
            foreach (var entry in config.Entries)
            {
                if (!string.IsNullOrEmpty(entry.AssemblyQualifiedName)) continue;
                if (!liveTypes.TryGetValue(entry.Name, out var t)) continue;
                entry.AssemblyQualifiedName = t.AssemblyQualifiedName;
                backfilled = true;
            }

            if (!removed && !added && !backfilled) return;

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }
}
