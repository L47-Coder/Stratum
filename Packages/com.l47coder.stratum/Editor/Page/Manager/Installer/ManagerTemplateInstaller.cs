using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class ManagerTemplateInstaller
    {
        private const string ManifestFileName = "manifest.json";
        private const string LeafMarkerFileName = "_leaf.json";

        [Serializable]
        public sealed class PackageInfo
        {
            public string id;
            public string displayName;
            public string description;
            public bool recommended;
        }

        [Serializable]
        private sealed class Manifest { public List<PackageInfo> packages; }

        private static List<PackageInfo> _cachedManifest;

        public static IReadOnlyList<PackageInfo> LoadManifest()
        {
            if (_cachedManifest != null) return _cachedManifest;

            var manifestAbs = ResolveSourceAbsolute(ManifestFileName);
            if (string.IsNullOrEmpty(manifestAbs) || !File.Exists(manifestAbs))
            {
                Debug.LogWarning($"[ManagerTemplateInstaller] manifest.json not found at {WorkbenchPaths.ManagerTemplatesFolder}/{ManifestFileName}.");
                _cachedManifest = new List<PackageInfo>();
                return _cachedManifest;
            }

            try
            {
                var json = File.ReadAllText(manifestAbs);
                var parsed = JsonUtility.FromJson<Manifest>(json);
                _cachedManifest = parsed?.packages ?? new List<PackageInfo>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ManagerTemplateInstaller] Failed to parse manifest.json: {ex.Message}");
                _cachedManifest = new List<PackageInfo>();
            }

            return _cachedManifest;
        }

        public static void InvalidateManifestCache() => _cachedManifest = null;

        public static bool IsPackageInstalled(string packageId)
        {
            var managerName = GetPackageName(packageId);
            if (string.IsNullOrEmpty(managerName)) return false;

            var rootAbs = ResolveAssetRootAbsolute(WorkbenchPaths.ManagerRoot);
            if (string.IsNullOrEmpty(rootAbs) || !Directory.Exists(rootAbs)) return false;

            try
            {
                var fileName = $"{managerName}Manager.cs";
                foreach (var _ in Directory.EnumerateFiles(rootAbs, fileName, SearchOption.AllDirectories))
                    return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ManagerTemplateInstaller] Failed to scan installed Managers: {ex.Message}");
            }

            return false;
        }

        public static int InstallPackages(IList<string> packageIds)
        {
            if (packageIds == null) return 0;

            var installed = 0;
            foreach (var id in packageIds)
            {
                var managerName = GetPackageName(id);
                if (string.IsNullOrEmpty(managerName) || IsPackageInstalled(id)) continue;

                if (!TryResolveTemplateFolder(id, out var sourcePath, out var relativeTemplatePath))
                {
                    Debug.LogError($"[ManagerTemplateInstaller] Template folder missing for \"{id}\" under {WorkbenchPaths.ManagerTemplatesFolder}.");
                    continue;
                }

                var destPath = $"{WorkbenchPaths.ManagerRoot}/{relativeTemplatePath}";

                AssetTransporter.Transfer(sourcePath, destPath);
                ManagerPostCompileAssetService.ScheduleTemplateInstall(
                    managerName,
                    $"{destPath}/{managerName}ManagerConfig.asset",
                    ManagerAddressConvention.AddressOf(managerName));
                installed++;
                Debug.Log($"[ManagerTemplateInstaller] Installed Manager template \"{id}\" to {destPath}.");
            }

            return installed;
        }

        private static bool TryResolveTemplateFolder(string packageId, out string sourceAssetPath, out string relativeTemplatePath)
        {
            sourceAssetPath = null;
            relativeTemplatePath = null;

            var rootAbs = ResolveSourceAbsolute();
            if (string.IsNullOrEmpty(rootAbs) || !Directory.Exists(rootAbs)) return false;

            var normalizedId = NormalizeAssetPath(packageId);
            if (!string.IsNullOrEmpty(normalizedId))
            {
                var directAbs = Path.Combine(rootAbs, normalizedId.Replace('/', Path.DirectorySeparatorChar));
                if (Directory.Exists(directAbs))
                    return SetTemplateResult(rootAbs, directAbs, out sourceAssetPath, out relativeTemplatePath);
            }

            var managerName = GetPackageName(packageId);
            if (string.IsNullOrEmpty(managerName)) return false;

            try
            {
                string fallbackAbs = null;
                var expectedScriptName = $"{managerName}Manager.cs";

                foreach (var dir in Directory.EnumerateDirectories(rootAbs, "*", SearchOption.AllDirectories))
                {
                    if (!string.Equals(Path.GetFileName(dir), managerName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    fallbackAbs ??= dir;

                    if (File.Exists(Path.Combine(dir, LeafMarkerFileName)) ||
                        File.Exists(Path.Combine(dir, expectedScriptName)))
                        return SetTemplateResult(rootAbs, dir, out sourceAssetPath, out relativeTemplatePath);
                }

                if (fallbackAbs != null)
                    return SetTemplateResult(rootAbs, fallbackAbs, out sourceAssetPath, out relativeTemplatePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ManagerTemplateInstaller] Failed to scan Manager templates: {ex.Message}");
            }

            return false;
        }

        private static bool SetTemplateResult(string rootAbs, string sourceAbs, out string sourceAssetPath, out string relativeTemplatePath)
        {
            relativeTemplatePath = Rel(rootAbs, sourceAbs).Replace('\\', '/');
            sourceAssetPath = $"{WorkbenchPaths.ManagerTemplatesFolder}/{relativeTemplatePath}";
            return !string.IsNullOrEmpty(relativeTemplatePath);
        }

        private static string ResolveSourceAbsolute(string relativeInsideTemplate = null)
        {
            try
            {
                var root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", WorkbenchPaths.ManagerTemplatesFolder));
                return string.IsNullOrEmpty(relativeInsideTemplate)
                    ? root
                    : Path.Combine(root, relativeInsideTemplate.Replace('/', Path.DirectorySeparatorChar));
            }
            catch { return null; }
        }

        private static string ResolveAssetRootAbsolute(string assetRoot)
        {
            try { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetRoot)); }
            catch { return null; }
        }

        private static string GetPackageName(string packageId)
        {
            var normalized = NormalizeAssetPath(packageId);
            if (string.IsNullOrEmpty(normalized)) return string.Empty;
            var lastSlash = normalized.LastIndexOf('/');
            return lastSlash >= 0 ? normalized[(lastSlash + 1)..] : normalized;
        }

        private static string NormalizeAssetPath(string path) =>
            string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/').Trim('/');

        private static string Rel(string rootAbs, string fullPath)
        {
            var root = Path.GetFullPath(rootAbs).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(fullPath);
            var cmp = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return full.StartsWith(root, cmp) ? full[root.Length..] : Path.GetFileName(full);
        }
    }
}
