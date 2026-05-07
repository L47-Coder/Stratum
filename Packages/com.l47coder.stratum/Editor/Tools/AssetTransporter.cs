using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class AssetTransporter
    {
        public static int Transfer(string sourcePath, string destPath)
        {
            var srcAbs = Path.GetFullPath(Abs(sourcePath));
            var dstAbs = Path.GetFullPath(Abs(destPath));
            var isFile = File.Exists(srcAbs);

            if (!isFile && !Directory.Exists(srcAbs))
                return Err($"Source not found: {sourcePath}");
            if (File.Exists(dstAbs))
                return Err($"Destination is an existing file: {destPath}");
            if (!string.IsNullOrEmpty(Path.GetExtension(Path.GetFileName(destPath.TrimEnd('/', '\\')))))
                return Err($"Destination must be a folder path: {destPath}");

            Directory.CreateDirectory(dstAbs);

            var dstNorm = destPath.TrimEnd('/', '\\').Replace('\\', '/');
            var transferred = new List<string>();

            AssetDatabase.StartAssetEditing();
            try
            {
                if (isFile)
                {
                    var dst = Path.Combine(dstAbs, Path.GetFileName(srcAbs));
                    if (!File.Exists(dst)) { File.Copy(srcAbs, dst); transferred.Add(dstNorm + "/" + Path.GetFileName(srcAbs)); }
                }
                else
                {
                    var anyDir = false;
                    foreach (var d in Directory.EnumerateDirectories(srcAbs, "*", SearchOption.AllDirectories))
                    {
                        var dst = Path.Combine(dstAbs, Rel(srcAbs, d).Replace('/', Path.DirectorySeparatorChar));
                        if (!Directory.Exists(dst)) { Directory.CreateDirectory(dst); anyDir = true; }
                    }

                    foreach (var f in Directory.EnumerateFiles(srcAbs, "*", SearchOption.AllDirectories))
                    {
                        if (f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                        if (Path.GetFileName(f).Equals(".gitkeep", StringComparison.OrdinalIgnoreCase)) continue;
                        var rel = Rel(srcAbs, f).Replace('\\', '/');
                        var dst = Path.Combine(dstAbs, rel.Replace('/', Path.DirectorySeparatorChar));
                        if (File.Exists(dst)) continue;
                        try { File.Copy(f, dst); transferred.Add(dstNorm + "/" + rel); }
                        catch (Exception ex) { Debug.LogWarning($"[AssetTransporter] Copy failed '{f}': {ex.Message}"); }
                    }

                    if (anyDir && transferred.Count == 0) { AssetDatabase.Refresh(); return 0; }
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }

            if (transferred.Count == 0) return 0;
            AssetDatabase.Refresh();
            return transferred.Count;
        }

        private static string Rel(string rootAbs, string fullPath)
        {
            var root = Path.GetFullPath(rootAbs).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(fullPath);
            var cmp = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return full.StartsWith(root, cmp) ? full.Substring(root.Length) : Path.GetFileName(full);
        }

        private static string Abs(string p) => Path.Combine(Application.dataPath, "..", p);
        private static int Err(string msg) { Debug.LogError($"[AssetTransporter] {msg}"); return 0; }
    }
}
