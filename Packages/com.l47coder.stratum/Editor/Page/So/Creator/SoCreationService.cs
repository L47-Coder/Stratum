using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class SoCreationService
    {
        public static void CreateSoType(SoCreatorState state)
        {
            if (state == null || !state.IsValid) return;
            CreateSoType(state.BuildPlan());
        }

        private static void CreateSoType(SoCreationPlan plan)
        {
            var typeAvailable = SoTypeIndex.TryFindClass(plan.SoClassName, out _);

            if (plan.ShouldCreateScript) WriteSoScript(plan);
            EnsureFolder(plan.EntityFolderPath);
            WriteLeafMarker(plan.LeafMarkerPath);
            AssetDatabase.Refresh();

            if (typeAvailable)
                EnsureAsset(plan.SoClassName, plan.FirstAssetFilePath);
            else
                SoPostCompileAssetService.ScheduleAssetCreation(plan);

            SoTypeIndex.Invalidate();
        }

        private static void WriteSoScript(SoCreationPlan plan)
        {
            EnsureFolder(Path.GetDirectoryName(plan.ScriptFilePath));

            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine($"public class {plan.SoClassName} : ScriptableObject");
            sb.AppendLine("{");
            sb.AppendLine();
            sb.AppendLine("}");

            File.WriteAllText(plan.ScriptFilePath, sb.ToString(), Encoding.UTF8);
        }

        private static void WriteLeafMarker(string leafMarkerAssetPath)
        {
            if (string.IsNullOrEmpty(leafMarkerAssetPath)) return;
            var abs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", leafMarkerAssetPath));
            var dir = Path.GetDirectoryName(abs);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (!File.Exists(abs)) File.WriteAllText(abs, string.Empty);
        }

        internal static bool EnsureAsset(string soClassName, string assetPath)
        {
            if (!SoTypeIndex.TryFindClass(soClassName, out var soType))
            {
                Debug.LogError($"[SoCreationService] SO type not found: {soClassName}");
                return false;
            }

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
            {
                EnsureFolder(Path.GetDirectoryName(assetPath));
                asset = ScriptableObject.CreateInstance(soType);
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
            }

            SoTypeIndex.Invalidate();
            Debug.Log($"[SoCreationService] {soClassName} ready at {assetPath}.");
            return true;
        }

        public static bool CreateNewAsset(Type soType, string folderAssetPath, out string createdAssetPath)
        {
            createdAssetPath = null;
            if (soType == null || string.IsNullOrEmpty(folderAssetPath)) return false;

            var baseName = $"New{StripSoSuffix(soType.Name)}";
            var uniqueName = SoAssetScanner.ComputeUniqueAssetName(folderAssetPath, baseName);
            var assetPath = $"{folderAssetPath.Replace('\\', '/').TrimEnd('/')}/{uniqueName}.asset";

            EnsureFolder(folderAssetPath);
            var asset = ScriptableObject.CreateInstance(soType);
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();

            createdAssetPath = assetPath;
            return true;
        }

        public static bool DeleteAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            return AssetDatabase.DeleteAsset(assetPath);
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

        private static string StripSoSuffix(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return typeName;
            const string suffix = "SO";
            return typeName.EndsWith(suffix, StringComparison.Ordinal) && typeName.Length > suffix.Length
                ? typeName[..^suffix.Length]
                : typeName;
        }
    }

    internal static class SoPostCompileAssetService
    {
        [InitializeOnLoadMethod]
        private static void ProcessPendingAssetCreation()
        {
            var soClassName = SessionState.GetString(SoCreatorState.SessionSoClassNameKey, string.Empty);
            var assetPath = SessionState.GetString(SoCreatorState.SessionAssetPathKey, string.Empty);

            if (string.IsNullOrEmpty(soClassName) || string.IsNullOrEmpty(assetPath))
                return;

            SessionState.EraseString(SoCreatorState.SessionSoClassNameKey);
            SessionState.EraseString(SoCreatorState.SessionAssetPathKey);

            EditorApplication.delayCall += () =>
                SoCreationService.EnsureAsset(soClassName, assetPath);
        }

        public static void ScheduleAssetCreation(SoCreationPlan plan)
        {
            SessionState.SetString(SoCreatorState.SessionSoClassNameKey, plan.SoClassName);
            SessionState.SetString(SoCreatorState.SessionAssetPathKey, plan.FirstAssetFilePath);
        }
    }
}
