using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
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

            var settings = AddressableAssetSettingsDefaultObject.Settings;

            foreach (var file in Directory.EnumerateFiles(abs, "*.asset", SearchOption.TopDirectoryOnly))
            {
                var assetPath = $"{folder}/{Path.GetFileName(file)}";
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (so == null || !expectedType.IsInstanceOfType(so)) continue;

                var name = Path.GetFileNameWithoutExtension(assetPath);
                var expected = SoAddressConvention.AddressOf(expectedType, name);
                var actual = ResolveActualAddress(settings, assetPath);

                rows.Add(new SoRow
                {
                    Name = name,
                    Address = string.IsNullOrEmpty(actual) ? expected : actual,
                    Target = so,
                    AssetPath = assetPath,
                });
            }

            rows.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return rows;
        }

        private static string ResolveActualAddress(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings, string assetPath)
        {
            if (settings == null) return null;
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) return null;
            var entry = settings.FindAssetEntry(guid);
            return entry?.address;
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
