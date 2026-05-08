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
        public const string GeneratedFolderName = "Generated";

        public const string SessionManagerNameKey = "ManagerCreator.ManagerName";
        public const string SessionAssetPathKey = "ManagerCreator.AssetPath";
        public const string SessionAssetAddressKey = "ManagerCreator.AssetAddress";

        private static readonly Regex ValidManagerNameRegex = new(@"^[A-Z][a-zA-Z0-9]*$", RegexOptions.Compiled);

        public string InputManagerName { get; private set; } = string.Empty;
        public bool IncludeConfig { get; private set; } = true;
        public bool IsValid { get; private set; }
        public bool HasPreview => !string.IsNullOrEmpty(ManagerClassName);
        public string ErrorMessage { get; private set; } = string.Empty;

        public string ManagerInterfaceName { get; private set; } = string.Empty;
        public string ManagerClassName { get; private set; } = string.Empty;
        public string ConfigClassName { get; private set; } = string.Empty;
        public string ManagerDataStructName { get; private set; } = string.Empty;
        public string ManagerTargetFilePath { get; private set; } = string.Empty;
        public string GeneratedFolderPath { get; private set; } = string.Empty;
        public string GeneratedManagerPartialFilePath { get; private set; } = string.Empty;
        public string GeneratedConfigFilePath { get; private set; } = string.Empty;
        public string GeneratedDataFilePath { get; private set; } = string.Empty;
        public string AssetTargetFilePath { get; private set; } = string.Empty;
        public string AddressableAddressName { get; private set; } = string.Empty;
        public string RefresherFilePath { get; private set; } = string.Empty;

        private string _parentFolderAssetPath = RootAssetPath;
        private string _existingManagerFilePath = string.Empty;
        private string _existingAssetPath = string.Empty;
        private string _existingRefresherFilePath = string.Empty;
        private bool _managerFileExists;
        private bool _managerClassExists;
        private bool _generatedFolderExists;
        private bool _assetExists;
        private bool _refresherFileExists;

        private PreviewItem[] _namePreviewItems = Array.Empty<PreviewItem>();
        private PreviewItem[] _pathPreviewItems = Array.Empty<PreviewItem>();
        private PreviewItem[] _addressablePreviewItems = Array.Empty<PreviewItem>();

        public void Reset()
        {
            InputManagerName = string.Empty;
            IncludeConfig = true;
            IsValid = false;
            ErrorMessage = string.Empty;
            _parentFolderAssetPath = RootAssetPath;
            ClearOutput();
        }

        public void SetInputManagerName(string managerName) => ApplyInput(managerName, RootAssetPath);

        public void SetInputWithParentFolder(string parentAssetPath, string managerName) => ApplyInput(managerName, parentAssetPath);

        public void SetIncludeConfig(bool includeConfig)
        {
            IncludeConfig = includeConfig;
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
            InputManagerName, IncludeConfig,
            ManagerInterfaceName, ManagerClassName, ConfigClassName, ManagerDataStructName,
            EntityFolderPath, ManagerTargetFilePath,
            GeneratedFolderPath, GeneratedManagerPartialFilePath,
            GeneratedConfigFilePath, GeneratedDataFilePath,
            AssetTargetFilePath, AddressableAddressName, RefresherFilePath,
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
            ConfigClassName = IncludeConfig ? $"{InputManagerName}ManagerConfig" : string.Empty;
            ManagerDataStructName = IncludeConfig ? $"{InputManagerName}ManagerData" : string.Empty;

            var entityFolder = $"{_parentFolderAssetPath}/{InputManagerName}";
            ManagerTargetFilePath = $"{entityFolder}/{ManagerClassName}.cs";
            GeneratedFolderPath = $"{entityFolder}/{GeneratedFolderName}";
            GeneratedManagerPartialFilePath = $"{GeneratedFolderPath}/{ManagerClassName}.Generated.cs";
            GeneratedConfigFilePath = IncludeConfig ? $"{GeneratedFolderPath}/{ConfigClassName}.cs" : string.Empty;
            GeneratedDataFilePath = IncludeConfig ? $"{GeneratedFolderPath}/{ManagerDataStructName}.cs" : string.Empty;
            AssetTargetFilePath = IncludeConfig ? $"{entityFolder}/{ConfigClassName}.asset" : string.Empty;
            AddressableAddressName = IncludeConfig ? ManagerAddressConvention.AddressOf(InputManagerName) : string.Empty;
            RefresherFilePath = IncludeConfig ? $"{entityFolder}/Editor/{InputManagerName}ManagerRefresher.cs" : string.Empty;

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
            GeneratedFolderPath = string.Empty;
            GeneratedManagerPartialFilePath = string.Empty;
            GeneratedConfigFilePath = string.Empty;
            GeneratedDataFilePath = string.Empty;
            AssetTargetFilePath = string.Empty;
            AddressableAddressName = string.Empty;
            RefresherFilePath = string.Empty;
            _existingManagerFilePath = string.Empty;
            _existingAssetPath = string.Empty;
            _existingRefresherFilePath = string.Empty;
            _managerFileExists = false;
            _managerClassExists = false;
            _generatedFolderExists = false;
            _assetExists = false;
            _refresherFileExists = false;
            _namePreviewItems = Array.Empty<PreviewItem>();
            _pathPreviewItems = Array.Empty<PreviewItem>();
            _addressablePreviewItems = Array.Empty<PreviewItem>();
        }

        private bool ShouldCreateManagerFile() => !_managerFileExists && !_managerClassExists;

        private PreviewStatus GetManagerCodeStatus() =>
            string.IsNullOrEmpty(ManagerClassName) ? PreviewStatus.Neutral : ShouldCreateManagerFile() ? PreviewStatus.Create : PreviewStatus.Skip;

        private PreviewStatus GetGeneratedCodeStatus() =>
            string.IsNullOrEmpty(GeneratedFolderPath) ? PreviewStatus.Neutral : _generatedFolderExists ? PreviewStatus.Write : PreviewStatus.Create;

        private PreviewStatus GetAssetStatus() =>
            !IncludeConfig || string.IsNullOrEmpty(AssetTargetFilePath) ? PreviewStatus.Neutral : _assetExists ? PreviewStatus.Write : PreviewStatus.Create;

        private PreviewStatus GetRefresherStatus() =>
            !IncludeConfig || string.IsNullOrEmpty(RefresherFilePath) ? PreviewStatus.Neutral : _refresherFileExists ? PreviewStatus.Skip : PreviewStatus.Create;

        private void RefreshPreviewCache()
        {
            RefreshExistingTargets();

            var managerStatus = GetManagerCodeStatus();
            var generatedStatus = GetGeneratedCodeStatus();
            var assetStatus = GetAssetStatus();
            var refresherStatus = GetRefresherStatus();

            _namePreviewItems = IncludeConfig
                ? new[]
                {
                    new PreviewItem("Interface",     ManagerInterfaceName, managerStatus),
                    new PreviewItem("Manager class", ManagerClassName,     managerStatus),
                    new PreviewItem("Config class",  ConfigClassName,      generatedStatus),
                    new PreviewItem("Data class",    ManagerDataStructName,generatedStatus),
                }
                : new[]
                {
                    new PreviewItem("Interface",     ManagerInterfaceName, managerStatus),
                    new PreviewItem("Manager class", ManagerClassName,     managerStatus),
                };

            _pathPreviewItems = IncludeConfig
                ? new[]
                {
                    new PreviewItem("Manager script",  _managerFileExists     ? _existingManagerFilePath   : ManagerTargetFilePath,  managerStatus),
                    new PreviewItem("Generated folder",GeneratedFolderPath,    generatedStatus),
                    new PreviewItem("Asset file",      _assetExists           ? _existingAssetPath         : AssetTargetFilePath,    assetStatus),
                    new PreviewItem("Refresher script",_refresherFileExists   ? _existingRefresherFilePath : RefresherFilePath,      refresherStatus),
                }
                : new[]
                {
                    new PreviewItem("Manager script",  _managerFileExists ? _existingManagerFilePath : ManagerTargetFilePath, managerStatus),
                    new PreviewItem("Generated folder",GeneratedFolderPath, generatedStatus),
                };

            _addressablePreviewItems = IncludeConfig
                ? new[]
                {
                    new PreviewItem("Addressable group",   AddressableGroupName,  assetStatus),
                    new PreviewItem("Addressable address", AddressableAddressName,assetStatus),
                }
                : new[]
                {
                    new PreviewItem("Addressable group",   "—",                 PreviewStatus.Neutral),
                    new PreviewItem("Addressable address", "(config disabled)", PreviewStatus.Neutral),
                };
        }

        private void RefreshExistingTargets()
        {
            _existingManagerFilePath = ResolveExisting(ManagerTargetFilePath,
                ManagerAssetIndex.FindManagerScript(Path.GetFileName(ManagerTargetFilePath)));

            _generatedFolderExists = !string.IsNullOrEmpty(GeneratedFolderPath) && FolderExists(GeneratedFolderPath);

            _existingAssetPath = IncludeConfig
                ? ResolveExisting(AssetTargetFilePath, ManagerAssetIndex.FindManagerAsset(Path.GetFileName(AssetTargetFilePath)))
                : string.Empty;

            _existingRefresherFilePath = string.Empty;
            if (!string.IsNullOrEmpty(RefresherFilePath) && FileExists(RefresherFilePath))
                _existingRefresherFilePath = RefresherFilePath;

            _managerFileExists = !string.IsNullOrEmpty(_existingManagerFilePath);
            _assetExists = !string.IsNullOrEmpty(_existingAssetPath);
            _refresherFileExists = !string.IsNullOrEmpty(_existingRefresherFilePath);
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
        public readonly bool IncludeConfig;
        public readonly string ManagerInterfaceName;
        public readonly string ManagerClassName;
        public readonly string ConfigClassName;
        public readonly string ManagerDataStructName;
        public readonly string EntityFolderPath;
        public readonly string ManagerTargetFilePath;
        public readonly string GeneratedFolderPath;
        public readonly string GeneratedManagerPartialFilePath;
        public readonly string GeneratedConfigFilePath;
        public readonly string GeneratedDataFilePath;
        public readonly string AssetFilePath;
        public readonly string AddressableAddressName;
        public readonly string RefresherFilePath;
        public readonly bool ShouldCreateManagerFile;

        public ManagerCreationPlan(
            string managerName, bool includeConfig,
            string managerInterfaceName, string managerClassName, string configClassName, string managerDataStructName,
            string entityFolderPath, string managerTargetFilePath,
            string generatedFolderPath, string generatedManagerPartialFilePath,
            string generatedConfigFilePath, string generatedDataFilePath,
            string assetFilePath, string addressableAddressName, string refresherFilePath,
            bool shouldCreateManagerFile)
        {
            ManagerName = managerName;
            IncludeConfig = includeConfig;
            ManagerInterfaceName = managerInterfaceName;
            ManagerClassName = managerClassName;
            ConfigClassName = configClassName;
            ManagerDataStructName = managerDataStructName;
            EntityFolderPath = entityFolderPath;
            ManagerTargetFilePath = managerTargetFilePath;
            GeneratedFolderPath = generatedFolderPath;
            GeneratedManagerPartialFilePath = generatedManagerPartialFilePath;
            GeneratedConfigFilePath = generatedConfigFilePath;
            GeneratedDataFilePath = generatedDataFilePath;
            AssetFilePath = assetFilePath;
            AddressableAddressName = addressableAddressName;
            RefresherFilePath = refresherFilePath;
            ShouldCreateManagerFile = shouldCreateManagerFile;
        }
    }
}
