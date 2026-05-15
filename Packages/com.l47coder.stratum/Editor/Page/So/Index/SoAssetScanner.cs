using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class SoAssetScanner
    {
        public static List<SoRow> Scan(string leafFolderAssetPath, Type expectedType)
        {
            var rows = new List<SoRow>();
            if (string.IsNullOrEmpty(leafFolderAssetPath) || expectedType == null) return rows;

            var folder = leafFolderAssetPath.Replace('\\', '/').TrimEnd('/');
            var abs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", folder));
            if (!Directory.Exists(abs)) return rows;

            foreach (var file in Directory.EnumerateFiles(abs, "*.asset", SearchOption.TopDirectoryOnly))
            {
                var assetPath = $"{folder}/{Path.GetFileName(file)}";
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (so == null || !expectedType.IsInstanceOfType(so)) continue;

                rows.Add(new SoRow
                {
                    Name = Path.GetFileNameWithoutExtension(assetPath),
                    Target = so,
                    AssetPath = assetPath,
                });
            }

            rows.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return rows;
        }

        public static string ComputeUniqueAssetName(string folderAssetPath, string baseName)
        {
            var folder = folderAssetPath.Replace('\\', '/').TrimEnd('/');
            var abs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", folder));
            if (!Directory.Exists(abs)) return baseName;

            var existing = new HashSet<string>(
                Directory.EnumerateFiles(abs, "*.asset", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileNameWithoutExtension),
                StringComparer.OrdinalIgnoreCase);

            if (!existing.Contains(baseName)) return baseName;
            for (var i = 1; i < 1000; i++)
            {
                var candidate = $"{baseName} ({i})";
                if (!existing.Contains(candidate)) return candidate;
            }
            return $"{baseName} ({Guid.NewGuid():N})";
        }
    }
}
