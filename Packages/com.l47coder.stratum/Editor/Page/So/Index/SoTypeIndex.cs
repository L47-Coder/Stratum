using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class SoTypeIndex
    {
        private static Dictionary<string, Type> _byFullName;
        private static readonly Dictionary<string, Type> _folderCache = new();

        static SoTypeIndex()
        {
            EditorApplication.projectChanged -= Invalidate;
            EditorApplication.projectChanged += Invalidate;
        }

        public static void Invalidate()
        {
            _byFullName = null;
            _folderCache.Clear();
        }

        public static Type ResolveType(string leafFolderAssetPath)
        {
            if (string.IsNullOrEmpty(leafFolderAssetPath)) return null;
            var key = leafFolderAssetPath.Replace('\\', '/').TrimEnd('/');
            if (_folderCache.TryGetValue(key, out var cached) && cached != null) return cached;

            var resolved = ResolveFromAssets(key) ?? ResolveFromScript(key);
            if (resolved != null) _folderCache[key] = resolved;
            return resolved;
        }

        public static bool TryFindClass(string className, out Type type)
        {
            EnsureIndex();

            if (_byFullName.TryGetValue(className, out type) && type != null) return true;

            type = null;
            foreach (var t in _byFullName.Values)
            {
                if (t.Name != className) continue;
                if (type != null)
                {
                    Debug.LogWarning($"[SoTypeIndex] Ambiguous class name '{className}': found '{type.FullName}' and '{t.FullName}'. Use the full name to disambiguate.");
                    type = null;
                    return false;
                }
                type = t;
            }
            return type != null;
        }

        public static IReadOnlyList<Type> AllSoTypes()
        {
            EnsureIndex();
            return _byFullName.Values.ToList();
        }

        private static Type ResolveFromAssets(string folderAssetPath)
        {
            var absFolder = ToAbsolute(folderAssetPath);
            if (!Directory.Exists(absFolder)) return null;

            foreach (var file in Directory.EnumerateFiles(absFolder, "*.asset", SearchOption.TopDirectoryOnly))
            {
                var assetPath = $"{folderAssetPath}/{Path.GetFileName(file)}";
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (so != null) return so.GetType();
            }
            return null;
        }

        private static Type ResolveFromScript(string folderAssetPath)
        {
            var absFolder = ToAbsolute(folderAssetPath);
            if (!Directory.Exists(absFolder)) return null;

            foreach (var file in Directory.EnumerateFiles(absFolder, "*.cs", SearchOption.TopDirectoryOnly))
            {
                var candidate = Path.GetFileNameWithoutExtension(file);
                if (TryFindClass(candidate, out var type) && IsScriptableObjectSubclass(type))
                    return type;
            }
            return null;
        }

        private static void EnsureIndex()
        {
            if (_byFullName != null) return;
            _byFullName = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var t in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
            {
                if (t.IsAbstract || t.IsInterface || t.FullName == null) continue;
                _byFullName[t.FullName] = t;
            }
        }

        private static bool IsScriptableObjectSubclass(Type t) =>
            t != null && !t.IsAbstract && !t.IsInterface && typeof(ScriptableObject).IsAssignableFrom(t);

        private static string ToAbsolute(string assetPath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
    }
}
