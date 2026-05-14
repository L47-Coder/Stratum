using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stratum;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class SoTypeIndex
    {
        private static Dictionary<string, Type> _byClassName;
        private static readonly Dictionary<string, Type> _folderCache = new();

        static SoTypeIndex()
        {
            EditorApplication.projectChanged -= Invalidate;
            EditorApplication.projectChanged += Invalidate;
        }

        public static void Invalidate()
        {
            _byClassName = null;
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
            return _byClassName.TryGetValue(className, out type) && type != null;
        }

        public static IReadOnlyList<Type> AllSoTypes()
        {
            EnsureIndex();
            return _byClassName.Values.ToList();
        }

        private static Type ResolveFromAssets(string folderAssetPath)
        {
            var absFolder = ToAbsolute(folderAssetPath);
            if (!Directory.Exists(absFolder)) return null;

            foreach (var file in Directory.EnumerateFiles(absFolder, "*.asset", SearchOption.TopDirectoryOnly))
            {
                var assetPath = $"{folderAssetPath}/{Path.GetFileName(file)}";
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (so is BaseSo baseSo) return baseSo.GetType();
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
                if (TryFindClass(candidate, out var type) && IsBaseSoSubclass(type))
                    return type;
            }
            return null;
        }

        private static void EnsureIndex()
        {
            if (_byClassName != null) return;
            _byClassName = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var t in TypeCache.GetTypesDerivedFrom<BaseSo>())
            {
                if (t.IsAbstract || t.IsInterface) continue;
                _byClassName[t.Name] = t;
            }
        }

        private static bool IsBaseSoSubclass(Type t) =>
            t != null && !t.IsAbstract && !t.IsInterface && typeof(BaseSo).IsAssignableFrom(t);

        private static string ToAbsolute(string assetPath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
    }
}
