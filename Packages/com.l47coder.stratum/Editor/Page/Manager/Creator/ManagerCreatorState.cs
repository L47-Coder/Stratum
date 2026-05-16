using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class ManagerCreatorState
    {
        public const string RootAssetPath = WorkbenchPaths.ManagerRoot;
        public const string AddressableGroupName = "ManagerConfig";
        private const string GeneratedFolderName = "Generated";

        public const string SessionManagerNameKey = "ManagerCreator.ManagerName";
        public const string SessionAssetPathKey = "ManagerCreator.AssetPath";
        public const string SessionAssetAddressKey = "ManagerCreator.AssetAddress";

        private const string ManagerAssemblyName = "Game.Managers";
        private static readonly Regex ValidManagerNameRegex = new(@"^[A-Z][a-zA-Z0-9]*$", RegexOptions.Compiled);

        public string InputManagerName { get; private set; } = string.Empty;
        public bool IsValid { get; private set; }
        public bool HasPreview => !string.IsNullOrEmpty(ManagerClassName);
        public string ErrorMessage { get; private set; } = string.Empty;

        public string ManagerInterfaceName { get; private set; } = string.Empty;
        public string ManagerClassName { get; private set; } = string.Empty;
        public string ConfigClassName { get; private set; } = string.Empty;
        public string ManagerDataStructName { get; private set; } = string.Empty;
        public string ManagerTargetFilePath { get; private set; } = string.Empty;
        public string DataTargetFilePath { get; private set; } = string.Empty;
        public string GeneratedFolderPath { get; private set; } = string.Empty;
        public string GeneratedDataFilePath { get; private set; } = string.Empty;
        public string GeneratedConfigFilePath { get; private set; } = string.Empty;
        public string GeneratedManagerPartialFilePath { get; private set; } = string.Empty;
        public string EditorFolderPath { get; private set; } = string.Empty;
        public string EditorRefresherFilePath { get; private set; } = string.Empty;
        public string EditorAsmRefFilePath { get; private set; } = string.Empty;
        public string AssetTargetFilePath { get; private set; } = string.Empty;
        public string AddressableAddressName { get; private set; } = string.Empty;

        private string _parentFolderAssetPath = RootAssetPath;
        private string _existingManagerFilePath = string.Empty;
        private string _existingAssetPath = string.Empty;
        private bool _managerFileExists;
        private bool _managerClassExists;
        private bool _generatedFolderExists;
        private bool _editorFolderExists;
        private bool _assetExists;

        private PreviewItem[] _namePreviewItems = Array.Empty<PreviewItem>();
        private PreviewItem[] _pathPreviewItems = Array.Empty<PreviewItem>();
        private PreviewItem[] _addressablePreviewItems = Array.Empty<PreviewItem>();

        public void Reset()
        {
            InputManagerName = string.Empty;
            IsValid = false;
            ErrorMessage = string.Empty;
            ClearOutput();
        }

        public void SetInputManagerName(string managerName) => ApplyInput(managerName, _parentFolderAssetPath);

        public void SetInputWithParentFolder(string parentAssetPath, string managerName) => ApplyInput(managerName, parentAssetPath);

        public void SetParentFolder(string parentAssetPath)
        {
            if (!TryNormalizeParentFolder(parentAssetPath, out var normalizedParent, out var parentError))
            {
                ErrorMessage = parentError;
                IsValid = false;
                ClearOutput();
                return;
            }
            _parentFolderAssetPath = normalizedParent;
            RefreshDerivedState();
        }

        public void RefreshDerivedState()
        {
            if (string.IsNullOrWhiteSpace(InputManagerName))
            {
                ClearOutput();
                ErrorMessage = string.Empty;
                IsValid = false;
                return;
            }
            ApplyInput(InputManagerName, _parentFolderAssetPath);
        }

        public PreviewItem[] GetNamePreviewItems() => _namePreviewItems;
        public PreviewItem[] GetPathPreviewItems() => _pathPreviewItems;
        public PreviewItem[] GetAddressablePreviewItems() => _addressablePreviewItems;
        public PreviewStatus GetInputStatus() => IsValid ? PreviewStatus.Create : PreviewStatus.Skip;

        public ManagerCreationPlan BuildPlan() => new(
            InputManagerName,
            ManagerInterfaceName, ManagerClassName, ConfigClassName, ManagerDataStructName,
            EntityFolderPath, ManagerTargetFilePath, DataTargetFilePath,
            GeneratedFolderPath, GeneratedDataFilePath, GeneratedConfigFilePath, GeneratedManagerPartialFilePath,
            EditorFolderPath, EditorRefresherFilePath, EditorAsmRefFilePath,
            AssetTargetFilePath, AddressableAddressName,
            ShouldCreateManagerFile());

        private void ApplyInput(string managerName, string parentAssetPath)
        {
            if (string.IsNullOrWhiteSpace(managerName)) { Reset(); return; }

            InputManagerName = managerName;
            if (!TryNormalizeParentFolder(parentAssetPath, out var normalizedParent, out var parentError))
            {
                ErrorMessage = parentError;
                IsValid = false;
                ClearOutput();
                return;
            }

            var normalizedName = managerName.Trim();
            if (!ValidManagerNameRegex.IsMatch(normalizedName))
            {
                ErrorMessage = "Manager name must be PascalCase and contain only letters and digits.";
                IsValid = false;
                ClearOutput();
                return;
            }

            _parentFolderAssetPath = normalizedParent;
            InputManagerName = normalizedName;
            ErrorMessage = string.Empty;
            IsValid = true;

            ManagerInterfaceName = $"I{InputManagerName}Manager";
            ManagerClassName = $"{InputManagerName}Manager";
            ConfigClassName = $"{InputManagerName}ManagerConfig";
            ManagerDataStructName = $"{InputManagerName}ManagerData";

            var entityFolder = $"{_parentFolderAssetPath}/{InputManagerName}";
            ManagerTargetFilePath = $"{entityFolder}/{ManagerClassName}.cs";
            DataTargetFilePath = $"{entityFolder}/{ManagerDataStructName}.cs";
            GeneratedFolderPath = $"{entityFolder}/{GeneratedFolderName}";
            GeneratedDataFilePath = $"{GeneratedFolderPath}/{ManagerDataStructName}.Generated.cs";
            GeneratedConfigFilePath = $"{GeneratedFolderPath}/{ConfigClassName}.cs";
            GeneratedManagerPartialFilePath = $"{GeneratedFolderPath}/{ManagerClassName}.Generated.cs";
            EditorFolderPath = $"{entityFolder}/Editor";
            EditorRefresherFilePath = $"{EditorFolderPath}/{ManagerClassName}Refresher.cs";
            EditorAsmRefFilePath = $"{EditorFolderPath}/Game.Editor.asmref";
            AssetTargetFilePath = $"{entityFolder}/{ConfigClassName}.asset";
            AddressableAddressName = ManagerAddressConvention.AddressOf(InputManagerName);

            RefreshPreviewCache();
        }

        private static bool TryNormalizeParentFolder(string parentAssetPath, out string normalizedParentPath, out string errorMessage)
        {
            normalizedParentPath = parentAssetPath?.Replace('\\', '/').TrimEnd('/') ?? string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(normalizedParentPath))
            { errorMessage = "Invalid parent folder."; return false; }

            var normalizedRoot = RootAssetPath.Replace('\\', '/').TrimEnd('/');
            if (!normalizedParentPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            { errorMessage = "Parent folder must be inside the manager root."; return false; }

            EnsureFolder(normalizedParentPath);
            if (!AssetDatabase.IsValidFolder(normalizedParentPath))
            { errorMessage = "Invalid parent folder."; return false; }

            return true;
        }

        private void ClearOutput()
        {
            ManagerInterfaceName = string.Empty;
            ManagerClassName = string.Empty;
            ConfigClassName = string.Empty;
            ManagerDataStructName = string.Empty;
            ManagerTargetFilePath = string.Empty;
            DataTargetFilePath = string.Empty;
            GeneratedFolderPath = string.Empty;
            GeneratedDataFilePath = string.Empty;
            GeneratedConfigFilePath = string.Empty;
            GeneratedManagerPartialFilePath = string.Empty;
            EditorFolderPath = string.Empty;
            EditorRefresherFilePath = string.Empty;
            EditorAsmRefFilePath = string.Empty;
            AssetTargetFilePath = string.Empty;
            AddressableAddressName = string.Empty;
            _existingManagerFilePath = string.Empty;
            _existingAssetPath = string.Empty;
            _managerFileExists = false;
            _managerClassExists = false;
            _generatedFolderExists = false;
            _editorFolderExists = false;
            _assetExists = false;
            _namePreviewItems = Array.Empty<PreviewItem>();
            _pathPreviewItems = Array.Empty<PreviewItem>();
            _addressablePreviewItems = Array.Empty<PreviewItem>();
        }

        private bool ShouldCreateManagerFile() => !_managerFileExists && !_managerClassExists;

        private PreviewStatus GetManagerCodeStatus() =>
            string.IsNullOrEmpty(ManagerClassName) ? PreviewStatus.Neutral : ShouldCreateManagerFile() ? PreviewStatus.Create : PreviewStatus.Skip;

        private PreviewStatus GetGeneratedCodeStatus() =>
            string.IsNullOrEmpty(GeneratedFolderPath) ? PreviewStatus.Neutral : _generatedFolderExists ? PreviewStatus.Write : PreviewStatus.Create;

        private PreviewStatus GetEditorCodeStatus() =>
            string.IsNullOrEmpty(EditorFolderPath) ? PreviewStatus.Neutral : _editorFolderExists ? PreviewStatus.Write : PreviewStatus.Create;

        private PreviewStatus GetAssetStatus() =>
            string.IsNullOrEmpty(AssetTargetFilePath) ? PreviewStatus.Neutral : _assetExists ? PreviewStatus.Write : PreviewStatus.Create;

        private void RefreshPreviewCache()
        {
            RefreshExistingTargets();

            var managerStatus = GetManagerCodeStatus();
            var generatedStatus = GetGeneratedCodeStatus();
            var editorStatus = GetEditorCodeStatus();
            var assetStatus = GetAssetStatus();

            _namePreviewItems = new[]
            {
                new PreviewItem("Interface",     ManagerInterfaceName, managerStatus),
                new PreviewItem("Manager class", ManagerClassName,     managerStatus),
                new PreviewItem("Config class",  ConfigClassName,      generatedStatus),
                new PreviewItem("Data class",    ManagerDataStructName,generatedStatus),
            };

            _pathPreviewItems = new[]
            {
                new PreviewItem("Manager script",   _managerFileExists ? _existingManagerFilePath : ManagerTargetFilePath, managerStatus),
                new PreviewItem("Data script",      DataTargetFilePath, managerStatus),
                new PreviewItem("Generated folder", GeneratedFolderPath, generatedStatus),
                new PreviewItem("Editor folder",    EditorFolderPath,    editorStatus),
                new PreviewItem("Asset file",       _assetExists ? _existingAssetPath : AssetTargetFilePath,               assetStatus),
            };

            _addressablePreviewItems = new[]
            {
                new PreviewItem("Addressable group",   AddressableGroupName,  assetStatus),
                new PreviewItem("Addressable address", AddressableAddressName,assetStatus),
            };
        }

        private void RefreshExistingTargets()
        {
            _existingManagerFilePath = ResolveExisting(ManagerTargetFilePath,
                ManagerAssetIndex.FindManagerScript(Path.GetFileName(ManagerTargetFilePath)));

            _generatedFolderExists = !string.IsNullOrEmpty(GeneratedFolderPath) && FolderExists(GeneratedFolderPath);
            _editorFolderExists = !string.IsNullOrEmpty(EditorFolderPath) && FolderExists(EditorFolderPath);

            _existingAssetPath = ResolveExisting(AssetTargetFilePath,
                ManagerAssetIndex.FindManagerAsset(Path.GetFileName(AssetTargetFilePath)));

            _managerFileExists = !string.IsNullOrEmpty(_existingManagerFilePath);
            _assetExists = !string.IsNullOrEmpty(_existingAssetPath);
            _managerClassExists = TypeExists(ManagerClassName);
        }

        private static string ResolveExisting(string preferredPath, string indexedPath)
        {
            if (FileExists(preferredPath)) return preferredPath;
            return FileExists(indexedPath) ? indexedPath : string.Empty;
        }

        private static bool FileExists(string assetPath) =>
            !string.IsNullOrEmpty(assetPath) && File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath)));

        private static bool FolderExists(string assetPath) =>
            !string.IsNullOrEmpty(assetPath) && Directory.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath)));

        private static void EnsureFolder(string assetPath)
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

        private static bool TypeExists(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return false;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!string.Equals(assembly.GetName().Name, ManagerAssemblyName, StringComparison.Ordinal))
                    continue;

                try { foreach (var t in assembly.GetTypes()) if (t.Name == typeName) return true; }
                catch (ReflectionTypeLoadException e)
                { foreach (var t in e.Types) if (t != null && t.Name == typeName) return true; }
                catch { }
            }

            return false;
        }

        private string EntityFolderPath => Path.GetDirectoryName(ManagerTargetFilePath)?.Replace('\\', '/');
    }

    internal readonly struct ManagerCreationPlan
    {
        public readonly string ManagerName;
        public readonly string ManagerInterfaceName;
        public readonly string ManagerClassName;
        public readonly string ConfigClassName;
        public readonly string ManagerDataStructName;
        public readonly string EntityFolderPath;
        public readonly string ManagerTargetFilePath;
        public readonly string DataTargetFilePath;
        public readonly string GeneratedFolderPath;
        public readonly string GeneratedDataFilePath;
        public readonly string GeneratedConfigFilePath;
        public readonly string GeneratedManagerPartialFilePath;
        public readonly string EditorFolderPath;
        public readonly string EditorRefresherFilePath;
        public readonly string EditorAsmRefFilePath;
        public readonly string AssetFilePath;
        public readonly string AddressableAddressName;
        public readonly bool ShouldCreateManagerFile;

        public ManagerCreationPlan(
            string managerName,
            string managerInterfaceName, string managerClassName, string configClassName, string managerDataStructName,
            string entityFolderPath, string managerTargetFilePath, string dataTargetFilePath,
            string generatedFolderPath, string generatedDataFilePath, string generatedConfigFilePath, string generatedManagerPartialFilePath,
            string editorFolderPath, string editorRefresherFilePath, string editorAsmRefFilePath,
            string assetFilePath, string addressableAddressName,
            bool shouldCreateManagerFile)
        {
            ManagerName = managerName;
            ManagerInterfaceName = managerInterfaceName;
            ManagerClassName = managerClassName;
            ConfigClassName = configClassName;
            ManagerDataStructName = managerDataStructName;
            EntityFolderPath = entityFolderPath;
            ManagerTargetFilePath = managerTargetFilePath;
            DataTargetFilePath = dataTargetFilePath;
            GeneratedFolderPath = generatedFolderPath;
            GeneratedDataFilePath = generatedDataFilePath;
            GeneratedConfigFilePath = generatedConfigFilePath;
            GeneratedManagerPartialFilePath = generatedManagerPartialFilePath;
            EditorFolderPath = editorFolderPath;
            EditorRefresherFilePath = editorRefresherFilePath;
            EditorAsmRefFilePath = editorAsmRefFilePath;
            AssetFilePath = assetFilePath;
            AddressableAddressName = addressableAddressName;
            ShouldCreateManagerFile = shouldCreateManagerFile;
        }
    }
}
