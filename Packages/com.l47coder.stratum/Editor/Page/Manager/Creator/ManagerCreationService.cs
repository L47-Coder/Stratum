using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class ManagerCreationService
    {
        public static void CreateManager(ManagerCreatorState state)
        {
            if (state == null || !state.IsValid) return;
            CreateManager(state.BuildPlan());
        }

        public static bool TryCreateManagerInParentFolder(string parentAssetPath, string className, out string errorMessage)
        {
            var state = new ManagerCreatorState();
            state.SetInputWithParentFolder(parentAssetPath, className);
            if (!state.IsValid) { errorMessage = state.ErrorMessage; return false; }

            CreateManager(state);
            errorMessage = null;
            return true;
        }

        private static void CreateManager(ManagerCreationPlan plan)
        {
            if (plan.ShouldCreateManagerFile)
                WriteManagerCode(plan);

            AssetDatabase.Refresh();
            ManagerAssetIndex.Invalidate();
            Debug.Log($"[ManagerCreationService] {plan.ClassName} ready.");
        }

        private static void WriteManagerCode(ManagerCreationPlan plan)
        {
            EnsureFolder(Path.GetDirectoryName(plan.ScriptFilePath));

            var sb = new StringBuilder();
            sb.AppendLine("using Stratum;");
            sb.AppendLine();
            sb.AppendLine($"public interface {plan.InterfaceName} : IManager");
            sb.AppendLine("{");
            sb.AppendLine();
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine($"internal sealed class {plan.ClassName} : {plan.InterfaceName}");
            sb.AppendLine("{");
            sb.AppendLine();
            sb.AppendLine("}");

            File.WriteAllText(plan.ScriptFilePath, sb.ToString(), Encoding.UTF8);
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

    internal static class ManagerAssetIndex
    {
        private static Dictionary<string, string> _scripts;

        static ManagerAssetIndex()
        {
            EditorApplication.projectChanged -= Invalidate;
            EditorApplication.projectChanged += Invalidate;
        }

        public static string FindManagerScript(string fileName) =>
            Find(ref _scripts, ManagerCreatorState.RootAssetPath, fileName, ".cs");

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
