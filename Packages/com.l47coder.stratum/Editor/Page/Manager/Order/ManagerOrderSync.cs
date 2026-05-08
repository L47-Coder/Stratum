using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Stratum.Editor
{
    internal static class ManagerOrderSync
    {
        [EditorSync(-1000)]
        public static void Run()
        {
            var config = AssetDatabase.LoadAssetAtPath<ManagerOrderConfig>(WorkbenchPaths.ManagerOrder);
            Sync(config);
        }

        public static void Sync(ManagerOrderConfig config)
        {
            if (config == null) return;

            var liveTypes = TypeCache.GetTypesDerivedFrom<BaseManager>()
                .Where(t => !t.IsAbstract)
                .ToDictionary(t => t.Name, t => t, System.StringComparer.Ordinal);

            var removed = config.Entries.RemoveAll(e => !liveTypes.ContainsKey(e.Name)) > 0;

            var existing = new HashSet<string>(config.Entries.Select(e => e.Name), System.StringComparer.Ordinal);
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
