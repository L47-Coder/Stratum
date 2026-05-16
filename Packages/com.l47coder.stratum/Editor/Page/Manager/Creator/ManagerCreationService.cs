using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

        public static bool TryCreateManagerInParentFolder(string parentAssetPath, string managerName, out string errorMessage)
        {
            var state = new ManagerCreatorState();
            state.SetInputWithParentFolder(parentAssetPath, managerName);
            if (!state.IsValid) { errorMessage = state.ErrorMessage; return false; }

            CreateManager(state);
            errorMessage = null;
            return true;
        }

        private static void CreateManager(ManagerCreationPlan plan)
        {
            if (plan.ShouldCreateManagerFile) WriteManagerCode(plan);
            if (plan.ShouldCreateDataFile) WriteDataCode(plan);

            if (plan.ShouldCreateGeneratedDataFile || plan.ShouldCreateGeneratedConfigFile || plan.ShouldCreateGeneratedManagerPartialFile)
                EnsureFolder(plan.GeneratedFolderPath);

            if (plan.ShouldCreateGeneratedDataFile) WriteDataGeneratedCode(plan);
            if (plan.ShouldCreateGeneratedConfigFile) WriteConfigGeneratedCode(plan);
            if (plan.ShouldCreateGeneratedManagerPartialFile) WriteManagerPartialGeneratedCode(plan);

            if (plan.ShouldCreateEditorRefresherFile || plan.ShouldCreateEditorAsmRefFile)
                EnsureFolder(plan.EditorFolderPath);

            if (plan.ShouldCreateEditorRefresherFile) WriteEditorRefresherCode(plan);
            if (plan.ShouldCreateEditorAsmRefFile) WriteEditorAsmRef(plan);
            EnsureGameEditorAssemblyDefinition();
            EnsureGameManagersInternalsVisibleTo();

            if (!string.IsNullOrEmpty(plan.EntityFolderPath))
                WriteLeafMarker(plan.EntityFolderPath);

            AssetDatabase.Refresh();

            if (!plan.ShouldCreateAssetFile || FindType(plan.ConfigClassName) != null)
                EnsureAssetAndAddressable(plan.ManagerName, plan.AssetFilePath, plan.AddressableAddressName);
            else
                ManagerPostCompileAssetService.ScheduleAssetCreation(plan);

            ManagerAssetIndex.Invalidate();
        }

        private static void WriteManagerCode(ManagerCreationPlan plan)
        {
            EnsureFolder(Path.GetDirectoryName(plan.ManagerTargetFilePath));

            var sb = new StringBuilder();
            sb.AppendLine("using Stratum;");
            sb.AppendLine();
            sb.AppendLine($"public interface {plan.ManagerInterfaceName}");
            sb.AppendLine("{");
            sb.AppendLine();
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine($"internal sealed partial class {plan.ManagerClassName} : {plan.ManagerInterfaceName}");
            sb.AppendLine("{");
            sb.AppendLine();
            sb.AppendLine("}");

            File.WriteAllText(plan.ManagerTargetFilePath, sb.ToString(), Encoding.UTF8);
        }

        private static void WriteDataCode(ManagerCreationPlan plan)
        {
            if (string.IsNullOrEmpty(plan.DataTargetFilePath)) return;

            var sb = new StringBuilder();
            sb.AppendLine($"internal sealed partial class {plan.ManagerDataStructName}");
            sb.AppendLine("{");
            sb.AppendLine("    public string Key;");
            sb.AppendLine("}");

            File.WriteAllText(plan.DataTargetFilePath, sb.ToString(), Encoding.UTF8);
        }

        private static void WriteDataGeneratedCode(ManagerCreationPlan plan)
        {
            if (string.IsNullOrEmpty(plan.GeneratedDataFilePath)) return;

            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using Stratum;");
            sb.AppendLine();
            sb.AppendLine("[Serializable]");
            sb.AppendLine($"internal partial class {plan.ManagerDataStructName} : BaseManagerData");
            sb.AppendLine("{");
            sb.AppendLine("    public override string GetKey() => Key;");
            sb.AppendLine("}");

            File.WriteAllText(plan.GeneratedDataFilePath, sb.ToString(), Encoding.UTF8);
        }

        private static void WriteConfigGeneratedCode(ManagerCreationPlan plan)
        {
            if (string.IsNullOrEmpty(plan.GeneratedConfigFilePath)) return;

            var sb = new StringBuilder();
            sb.AppendLine("using Stratum;");
            sb.AppendLine();
            sb.AppendLine($"internal partial class {plan.ConfigClassName} : BaseManagerConfig<{plan.ManagerDataStructName}> {{ }}");

            File.WriteAllText(plan.GeneratedConfigFilePath, sb.ToString(), Encoding.UTF8);
        }

        private static void WriteManagerPartialGeneratedCode(ManagerCreationPlan plan)
        {
            if (string.IsNullOrEmpty(plan.GeneratedManagerPartialFilePath)) return;

            var sb = new StringBuilder();
            sb.AppendLine("using Stratum;");
            sb.AppendLine("using UnityEngine.Scripting;");
            sb.AppendLine();
            sb.AppendLine("[Preserve]");
            sb.AppendLine($"internal partial class {plan.ManagerClassName} : BaseManager<{plan.ConfigClassName}, {plan.ManagerDataStructName}>");
            sb.AppendLine("{");
            sb.AppendLine($"    public override string AddressPath => \"{EscapeCSharpStringLiteral(plan.AddressableAddressName)}\";");
            sb.AppendLine("}");

            File.WriteAllText(plan.GeneratedManagerPartialFilePath, sb.ToString(), Encoding.UTF8);
        }

        private static void WriteEditorRefresherCode(ManagerCreationPlan plan)
        {
            if (string.IsNullOrEmpty(plan.EditorRefresherFilePath)) return;

            var sb = new StringBuilder();
            sb.AppendLine("using Stratum;");
            sb.AppendLine();
            sb.AppendLine($"internal static class {plan.ManagerClassName}Refresher");
            sb.AppendLine("{");
            sb.AppendLine("    [EditorSync]");
            sb.AppendLine("    public static void Run()");
            sb.AppendLine("    {");
            sb.AppendLine();
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(plan.EditorRefresherFilePath, sb.ToString(), Encoding.UTF8);
        }

        private static void WriteEditorAsmRef(ManagerCreationPlan plan)
        {
            if (string.IsNullOrEmpty(plan.EditorAsmRefFilePath)) return;
            File.WriteAllText(plan.EditorAsmRefFilePath, "{\n    \"reference\": \"Game.Editor\"\n}\n", Encoding.UTF8);
        }

        private static void EnsureGameEditorAssemblyDefinition()
        {
            var path = $"{WorkbenchPaths.GameRoot}/Editor/Game.Editor.asmdef";
            var abs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            if (File.Exists(abs)) return;

            EnsureFolder(Path.GetDirectoryName(path));
            File.WriteAllText(abs, BuildGameEditorAsmdef(), Encoding.UTF8);
        }

        private static string BuildGameEditorAsmdef()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("    \"name\": \"Game.Editor\",");
            sb.AppendLine("    \"rootNamespace\": \"\",");
            sb.AppendLine("    \"references\": [");
            sb.AppendLine("        \"Stratum\",");
            sb.AppendLine("        \"Stratum.Editor\",");
            sb.AppendLine("        \"Game.Managers\",");
            sb.AppendLine("        \"Game.Frame\",");
            sb.AppendLine("        \"Game.ScriptableObject\",");
            sb.AppendLine("        \"UniTask\",");
            sb.AppendLine("        \"UniTask.Addressables\",");
            sb.AppendLine("        \"Unity.Addressables\",");
            sb.AppendLine("        \"Unity.Addressables.Editor\",");
            sb.AppendLine("        \"Unity.ResourceManager\",");
            sb.AppendLine("        \"VContainer\"");
            sb.AppendLine("    ],");
            sb.AppendLine("    \"includePlatforms\": [");
            sb.AppendLine("        \"Editor\"");
            sb.AppendLine("    ],");
            sb.AppendLine("    \"excludePlatforms\": [],");
            sb.AppendLine("    \"allowUnsafeCode\": false,");
            sb.AppendLine("    \"overrideReferences\": false,");
            sb.AppendLine("    \"precompiledReferences\": [],");
            sb.AppendLine("    \"autoReferenced\": false,");
            sb.AppendLine("    \"defineConstraints\": [],");
            sb.AppendLine("    \"versionDefines\": [],");
            sb.AppendLine("    \"noEngineReferences\": false");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void EnsureGameManagersInternalsVisibleTo()
        {
            var path = $"{WorkbenchPaths.ManagerRoot}/Game.Managers.InternalsVisibleTo.cs";
            var abs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            if (File.Exists(abs)) return;

            EnsureFolder(Path.GetDirectoryName(path));
            File.WriteAllText(abs, "using System.Runtime.CompilerServices;\n\n[assembly: InternalsVisibleTo(\"Game.Editor\")]\n", Encoding.UTF8);
        }

        private static void WriteLeafMarker(string entityFolderAssetPath)
        {
            var abs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", entityFolderAssetPath, "_leaf.json"));
            var dir = Path.GetDirectoryName(abs);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (!File.Exists(abs)) File.WriteAllText(abs, string.Empty);
        }

        internal static bool EnsureAssetAndAddressable(string managerName, string assetPath, string assetAddress)
        {
            if (AssetFileExists(assetPath))
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            }
            else
            {
                EnsureFolder(Path.GetDirectoryName(assetPath));

                var configType = FindType($"{managerName}ManagerConfig");
                if (configType == null)
                {
                    Debug.LogError($"[ManagerCreationService] Config type not found: {managerName}ManagerConfig");
                    return false;
                }

                var asset = ScriptableObject.CreateInstance(configType);
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
            }

            AddressablesHelper.EnsureEntry(assetPath, assetAddress, ManagerCreatorState.AddressableGroupName);
            ManagerAssetIndex.Invalidate();
            Debug.Log($"[ManagerCreationService] {managerName}Manager ready.");
            return true;
        }

        private static bool AssetFileExists(string assetPath) =>
            !string.IsNullOrEmpty(assetPath) && File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath)));

        private static string EscapeCSharpStringLiteral(string value) =>
            string.IsNullOrEmpty(value) ? string.Empty : value.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static Type FindType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { foreach (var t in assembly.GetTypes()) if (t.Name == typeName) return t; }
                catch (ReflectionTypeLoadException e) { foreach (var t in e.Types) if (t != null && t.Name == typeName) return t; }
                catch { }
            }
            return null;
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

    internal static class ManagerPostCompileAssetService
    {
        private const string PendingTemplatesKey = "ManagerInstaller.PendingTemplates";

        [InitializeOnLoadMethod]
        private static void ProcessPendingAssetCreation()
        {
            var managerName = SessionState.GetString(ManagerCreatorState.SessionManagerNameKey, string.Empty);
            var assetPath = SessionState.GetString(ManagerCreatorState.SessionAssetPathKey, string.Empty);
            var assetAddress = SessionState.GetString(ManagerCreatorState.SessionAssetAddressKey, string.Empty);

            if (!string.IsNullOrEmpty(managerName) && !string.IsNullOrEmpty(assetPath) && !string.IsNullOrEmpty(assetAddress))
            {
                SessionState.EraseString(ManagerCreatorState.SessionManagerNameKey);
                SessionState.EraseString(ManagerCreatorState.SessionAssetPathKey);
                SessionState.EraseString(ManagerCreatorState.SessionAssetAddressKey);

                EditorApplication.delayCall += () =>
                    ManagerCreationService.EnsureAssetAndAddressable(managerName, assetPath, assetAddress);
            }

            var pending = SessionState.GetString(PendingTemplatesKey, string.Empty);
            if (string.IsNullOrEmpty(pending)) return;

            SessionState.EraseString(PendingTemplatesKey);
            foreach (var record in pending.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = record.Split(new[] { '|' }, 3);
                if (parts.Length == 3)
                {
                    var n = parts[0];
                    var p = parts[1];
                    var a = parts[2];
                    if (string.IsNullOrEmpty(n) || string.IsNullOrEmpty(p) || string.IsNullOrEmpty(a)) continue;
                    EditorApplication.delayCall += () =>
                        ManagerCreationService.EnsureAssetAndAddressable(n, p, a);
                    continue;
                }

                foreach (var legacyName in record.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var n = legacyName;
                    var p = $"{ManagerCreatorState.RootAssetPath}/{n}/{n}ManagerConfig.asset";
                    var a = ManagerAddressConvention.AddressOf(n);
                    EditorApplication.delayCall += () =>
                        ManagerCreationService.EnsureAssetAndAddressable(n, p, a);
                }
            }
        }

        public static void ScheduleAssetCreation(ManagerCreationPlan plan)
        {
            SessionState.SetString(ManagerCreatorState.SessionManagerNameKey, plan.ManagerName);
            SessionState.SetString(ManagerCreatorState.SessionAssetPathKey, plan.AssetFilePath);
            SessionState.SetString(ManagerCreatorState.SessionAssetAddressKey, plan.AddressableAddressName);
        }

        public static void ScheduleTemplateInstall(string managerName, string assetPath, string assetAddress)
        {
            if (string.IsNullOrEmpty(managerName) || string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(assetAddress))
                return;

            var existing = SessionState.GetString(PendingTemplatesKey, string.Empty);
            var record = $"{managerName}|{assetPath.Replace('\\', '/')}|{assetAddress}";
            var records = string.IsNullOrEmpty(existing)
                ? new List<string>()
                : new List<string>(existing.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries));
            if (!records.Contains(record)) records.Add(record);
            SessionState.SetString(PendingTemplatesKey, string.Join("\n", records));
        }

        public static void ScheduleTemplateInstall(string managerName) =>
            ScheduleTemplateInstall(
                managerName,
                $"{ManagerCreatorState.RootAssetPath}/{managerName}/{managerName}ManagerConfig.asset",
                ManagerAddressConvention.AddressOf(managerName));
    }

    internal static class ManagerAssetIndex
    {
        private static Dictionary<string, string> _scripts;
        private static Dictionary<string, string> _assets;

        static ManagerAssetIndex()
        {
            EditorApplication.projectChanged -= Invalidate;
            EditorApplication.projectChanged += Invalidate;
        }

        public static string FindManagerScript(string fileName) =>
            Find(ref _scripts, ManagerCreatorState.RootAssetPath, fileName, ".cs");

        public static string FindManagerAsset(string fileName) =>
            Find(ref _assets, ManagerCreatorState.RootAssetPath, fileName, ".asset");

        public static void Invalidate() => (_scripts, _assets) = (null, null);

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
