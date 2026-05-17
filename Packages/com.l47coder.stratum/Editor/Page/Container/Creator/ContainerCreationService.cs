using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class ContainerCreationService
    {
        public static void CreateScript(ContainerCreatorState state)
        {
            if (state == null || !state.IsValid) return;
            CreateScript(state.BuildPlan());
        }

        private static void CreateScript(ContainerCreationPlan plan)
        {
            if (plan.ShouldCreateScript)
                WriteScript(plan);

            AssetDatabase.Refresh();
            ContainerAssetIndex.Invalidate();
            Debug.Log($"[ContainerCreationService] {plan.ClassName} ready.");
        }

        private static void WriteScript(ContainerCreationPlan plan)
        {
            EnsureFolder(Path.GetDirectoryName(plan.ScriptFilePath));
            File.WriteAllText(plan.ScriptFilePath, BuildSource(plan.ClassName), Encoding.UTF8);
        }

        public static string BuildSource(string className)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine($"public class {className} : MonoBehaviour");
            sb.AppendLine("{");
            sb.AppendLine();
            sb.AppendLine("}");
            return sb.ToString();
        }

        internal static void EnsureFolder(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return;
            assetPath = assetPath.Replace('\\', '/').TrimEnd('/');
            if (assetPath == "Assets" || AssetDatabase.IsValidFolder(assetPath)) return;

            var parts = assetPath.Split('/');
            for (var i = 1; i < parts.Length; i++)
            {
                var parent = string.Join("/", parts, 0, i);
                var child = $"{parent}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(child))
                    AssetDatabase.CreateFolder(parent, parts[i]);
            }
        }
    }

    internal static class ContainerAssetIndex
    {
        private static Dictionary<string, string> _scripts;

        static ContainerAssetIndex()
        {
            EditorApplication.projectChanged -= Invalidate;
            EditorApplication.projectChanged += Invalidate;
        }

        public static string FindScript(string fileName) =>
            Find(ref _scripts, ContainerCreatorState.RootAssetPath, fileName, ".cs");

        public static void Invalidate() => _scripts = null;

        private static string Find(ref Dictionary<string, string> cache, string root, string fileName, string ext)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            cache ??= BuildIndex(root, ext);
            return cache.TryGetValue(fileName, out var path) ? path : null;
        }

        private static Dictionary<string, string> BuildIndex(string root, string ext)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var guid in AssetDatabase.FindAssets(string.Empty, new[] { root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(path) || !path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) continue;
                var name = Path.GetFileName(path);
                if (!map.ContainsKey(name)) map.Add(name, path);
            }
            return map;
        }
    }
}
