using System;
using System.IO;
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
        private string _existingDataFilePath = string.Empty;
        private string _existingAssetPath = string.Empty;
        private bool _managerFileExists;
        private bool _dataFileExists;
        private bool _generatedDataFileExists;
        private bool _generatedConfigFileExists;
        private bool _generatedManagerPartialFileExists;
        private bool _editorRefresherFileExists;
        private bool _editorAsmRefFileExists;
        private bool _assetExists;
        private bool _addressableEntryExists;

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

        public ManagerCreationPlan BuildPlan()
        {
            RefreshExistingTargets();
            return new ManagerCreationPlan(
                InputManagerName,
                ManagerInterfaceName, ManagerClassName, ConfigClassName, ManagerDataStructName,
                EntityFolderPath, ManagerTargetFilePath, DataTargetFilePath,
                GeneratedFolderPath, GeneratedDataFilePath, GeneratedConfigFilePath, GeneratedManagerPartialFilePath,
                EditorFolderPath, EditorRefresherFilePath, EditorAsmRefFilePath,
                ResolvedAssetFilePath, AddressableAddressName,
                ShouldCreateManagerFile(), ShouldCreateDataFile(),
                ShouldCreateGeneratedDataFile(), ShouldCreateGeneratedConfigFile(), ShouldCreateGeneratedManagerPartialFile(),
                ShouldCreateEditorRefresherFile(), ShouldCreateEditorAsmRefFile(),
                ShouldCreateAssetFile());
        }

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
            _existingDataFilePath = string.Empty;
            _existingAssetPath = string.Empty;
            _managerFileExists = false;
            _dataFileExists = false;
            _generatedDataFileExists = false;
            _generatedConfigFileExists = false;
            _generatedManagerPartialFileExists = false;
            _editorRefresherFileExists = false;
            _editorAsmRefFileExists = false;
            _assetExists = false;
            _addressableEntryExists = false;
            _namePreviewItems = Array.Empty<PreviewItem>();
            _pathPreviewItems = Array.Empty<PreviewItem>();
            _addressablePreviewItems = Array.Empty<PreviewItem>();
        }

        private bool ShouldCreateManagerFile() => !_managerFileExists;
        private bool ShouldCreateDataFile() => !_dataFileExists;
        private bool ShouldCreateGeneratedDataFile() => !_generatedDataFileExists;
        private bool ShouldCreateGeneratedConfigFile() => !_generatedConfigFileExists;
        private bool ShouldCreateGeneratedManagerPartialFile() => !_generatedManagerPartialFileExists;
        private bool ShouldCreateEditorRefresherFile() => !_editorRefresherFileExists;
        private bool ShouldCreateEditorAsmRefFile() => !_editorAsmRefFileExists;
        private bool ShouldCreateAssetFile() => !_assetExists;

        private static PreviewStatus GetFileStatus(string path, bool exists) =>
            string.IsNullOrEmpty(path) ? PreviewStatus.Neutral : exists ? PreviewStatus.Skip : PreviewStatus.Create;

        private static PreviewStatus MergeStatus(params PreviewStatus[] statuses)
        {
            foreach (var status in statuses)
                if (status == PreviewStatus.Create)
                    return PreviewStatus.Create;

            foreach (var status in statuses)
                if (status == PreviewStatus.Skip)
                    return PreviewStatus.Skip;

            return PreviewStatus.Neutral;
        }

        private void RefreshPreviewCache()
        {
            RefreshExistingTargets();

            var managerStatus = GetFileStatus(ManagerTargetFilePath, _managerFileExists);
            var dataStatus = GetFileStatus(DataTargetFilePath, _dataFileExists);
            var generatedDataStatus = GetFileStatus(GeneratedDataFilePath, _generatedDataFileExists);
            var generatedConfigStatus = GetFileStatus(GeneratedConfigFilePath, _generatedConfigFileExists);
            var generatedManagerStatus = GetFileStatus(GeneratedManagerPartialFilePath, _generatedManagerPartialFileExists);
            var refresherStatus = GetFileStatus(EditorRefresherFilePath, _editorRefresherFileExists);
            var asmRefStatus = GetFileStatus(EditorAsmRefFilePath, _editorAsmRefFileExists);
            var assetStatus = GetFileStatus(AssetTargetFilePath, _assetExists);
            var addressableStatus = GetFileStatus(ResolvedAssetFilePath, _addressableEntryExists);

            _namePreviewItems = new[]
            {
                new PreviewItem("Interface",     ManagerInterfaceName, managerStatus),
                new PreviewItem("Manager class", ManagerClassName,     managerStatus),
                new PreviewItem("Config class",  ConfigClassName,      generatedConfigStatus),
                new PreviewItem("Data class",    ManagerDataStructName,MergeStatus(dataStatus, generatedDataStatus)),
            };

            _pathPreviewItems = new[]
            {
                new PreviewItem("Manager script",   _managerFileExists ? _existingManagerFilePath : ManagerTargetFilePath, managerStatus),
                new PreviewItem("Data script",      _dataFileExists ? _existingDataFilePath : DataTargetFilePath, dataStatus),
                new PreviewItem("Generated manager", GeneratedManagerPartialFilePath, generatedManagerStatus),
                new PreviewItem("Generated data",   GeneratedDataFilePath, generatedDataStatus),
                new PreviewItem("Generated config", GeneratedConfigFilePath, generatedConfigStatus),
                new PreviewItem("Refresher script", EditorRefresherFilePath, refresherStatus),
                new PreviewItem("Editor asmref",    EditorAsmRefFilePath, asmRefStatus),
                new PreviewItem("Asset file",       _assetExists ? _existingAssetPath : AssetTargetFilePath,               assetStatus),
            };

            _addressablePreviewItems = new[]
            {
                new PreviewItem("Addressable group",   AddressableGroupName,  addressableStatus),
                new PreviewItem("Addressable address", AddressableAddressName,addressableStatus),
            };
        }

        private void RefreshExistingTargets()
        {
            _existingManagerFilePath = ResolveExisting(ManagerTargetFilePath,
                ManagerAssetIndex.FindManagerScript(Path.GetFileName(ManagerTargetFilePath)));

            _existingDataFilePath = ResolveExisting(DataTargetFilePath,
                ManagerAssetIndex.FindManagerScript(Path.GetFileName(DataTargetFilePath)));

            _generatedDataFileExists = FileExists(GeneratedDataFilePath);
            _generatedConfigFileExists = FileExists(GeneratedConfigFilePath);
            _generatedManagerPartialFileExists = FileExists(GeneratedManagerPartialFilePath);
            _editorRefresherFileExists = FileExists(EditorRefresherFilePath);
            _editorAsmRefFileExists = FileExists(EditorAsmRefFilePath);

            _existingAssetPath = ResolveExisting(AssetTargetFilePath,
                ManagerAssetIndex.FindManagerAsset(Path.GetFileName(AssetTargetFilePath)));

            _managerFileExists = !string.IsNullOrEmpty(_existingManagerFilePath);
            _dataFileExists = !string.IsNullOrEmpty(_existingDataFilePath);
            _assetExists = !string.IsNullOrEmpty(_existingAssetPath);
            _addressableEntryExists = _assetExists &&
                AddressablesHelper.HasEntry(_existingAssetPath, AddressableAddressName, AddressableGroupName);
        }

        private static string ResolveExisting(string preferredPath, string indexedPath)
        {
            if (FileExists(preferredPath)) return preferredPath;
            return FileExists(indexedPath) ? indexedPath : string.Empty;
        }

        private static bool FileExists(string assetPath) =>
            !string.IsNullOrEmpty(assetPath) && File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath)));

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

        private string EntityFolderPath => Path.GetDirectoryName(ManagerTargetFilePath)?.Replace('\\', '/');
        private string ResolvedAssetFilePath => _assetExists ? _existingAssetPath : AssetTargetFilePath;
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
        public readonly bool ShouldCreateDataFile;
        public readonly bool ShouldCreateGeneratedDataFile;
        public readonly bool ShouldCreateGeneratedConfigFile;
        public readonly bool ShouldCreateGeneratedManagerPartialFile;
        public readonly bool ShouldCreateEditorRefresherFile;
        public readonly bool ShouldCreateEditorAsmRefFile;
        public readonly bool ShouldCreateAssetFile;

        public ManagerCreationPlan(
            string managerName,
            string managerInterfaceName, string managerClassName, string configClassName, string managerDataStructName,
            string entityFolderPath, string managerTargetFilePath, string dataTargetFilePath,
            string generatedFolderPath, string generatedDataFilePath, string generatedConfigFilePath, string generatedManagerPartialFilePath,
            string editorFolderPath, string editorRefresherFilePath, string editorAsmRefFilePath,
            string assetFilePath, string addressableAddressName,
            bool shouldCreateManagerFile, bool shouldCreateDataFile,
            bool shouldCreateGeneratedDataFile, bool shouldCreateGeneratedConfigFile, bool shouldCreateGeneratedManagerPartialFile,
            bool shouldCreateEditorRefresherFile, bool shouldCreateEditorAsmRefFile,
            bool shouldCreateAssetFile)
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
            ShouldCreateDataFile = shouldCreateDataFile;
            ShouldCreateGeneratedDataFile = shouldCreateGeneratedDataFile;
            ShouldCreateGeneratedConfigFile = shouldCreateGeneratedConfigFile;
            ShouldCreateGeneratedManagerPartialFile = shouldCreateGeneratedManagerPartialFile;
            ShouldCreateEditorRefresherFile = shouldCreateEditorRefresherFile;
            ShouldCreateEditorAsmRefFile = shouldCreateEditorAsmRefFile;
            ShouldCreateAssetFile = shouldCreateAssetFile;
        }
    }
}
