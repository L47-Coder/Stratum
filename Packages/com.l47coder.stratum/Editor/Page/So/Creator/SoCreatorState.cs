using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Stratum.Editor
{
    internal sealed class SoCreatorState
    {
        public const string RootAssetPath = WorkbenchPaths.SoRoot;

        public const string SessionSoClassNameKey = "SoCreator.SoClassName";
        public const string SessionAssetPathKey = "SoCreator.AssetPath";
        public const string SessionAssetAddressKey = "SoCreator.AssetAddress";

        private static readonly Regex ValidNameRegex = new(@"^[A-Z][a-zA-Z0-9]*$", RegexOptions.Compiled);
        private const string ClassSuffix = "SO";

        public string InputName { get; private set; } = string.Empty;
        public bool IsValid { get; private set; }
        public bool HasPreview => !string.IsNullOrEmpty(SoClassName);
        public string ErrorMessage { get; private set; } = string.Empty;

        public string LogicalName { get; private set; } = string.Empty;
        public string SoClassName { get; private set; } = string.Empty;
        public string EntityFolderPath { get; private set; } = string.Empty;
        public string ScriptFilePath { get; private set; } = string.Empty;
        public string LeafMarkerPath { get; private set; } = string.Empty;
        public string FirstAssetFilePath { get; private set; } = string.Empty;
        public string FirstAssetAddress { get; private set; } = string.Empty;

        private string _parentFolderAssetPath = RootAssetPath;
        private bool _scriptExists;
        private bool _classExists;
        private bool _folderExists;
        private bool _firstAssetExists;

        private PreviewItem[] _namePreviewItems = Array.Empty<PreviewItem>();
        private PreviewItem[] _pathPreviewItems = Array.Empty<PreviewItem>();
        private PreviewItem[] _addressablePreviewItems = Array.Empty<PreviewItem>();

        public void Reset()
        {
            InputName = string.Empty;
            IsValid = false;
            ErrorMessage = string.Empty;
            ClearOutput();
        }

        public void SetInputName(string input) => ApplyInput(input, _parentFolderAssetPath);

        public void SetParentFolder(string parentAssetPath)
        {
            if (!TryNormalizeParentFolder(parentAssetPath, out var normalized, out var err))
            {
                ErrorMessage = err;
                IsValid = false;
                ClearOutput();
                return;
            }
            _parentFolderAssetPath = normalized;
            RefreshDerivedState();
        }

        public void RefreshDerivedState()
        {
            if (string.IsNullOrWhiteSpace(InputName))
            {
                ClearOutput();
                ErrorMessage = string.Empty;
                IsValid = false;
                return;
            }
            ApplyInput(InputName, _parentFolderAssetPath);
        }

        public PreviewItem[] GetNamePreviewItems() => _namePreviewItems;
        public PreviewItem[] GetPathPreviewItems() => _pathPreviewItems;
        public PreviewItem[] GetAddressablePreviewItems() => _addressablePreviewItems;
        public PreviewStatus GetInputStatus() => IsValid ? PreviewStatus.Create : PreviewStatus.Skip;

        public SoCreationPlan BuildPlan() => new(
            LogicalName, SoClassName,
            EntityFolderPath, ScriptFilePath, LeafMarkerPath,
            FirstAssetFilePath, FirstAssetAddress,
            ShouldCreateScript());

        private void ApplyInput(string input, string parentAssetPath)
        {
            if (string.IsNullOrWhiteSpace(input)) { Reset(); return; }

            InputName = input;
            if (!TryNormalizeParentFolder(parentAssetPath, out var normalizedParent, out var parentErr))
            {
                ErrorMessage = parentErr;
                IsValid = false;
                ClearOutput();
                return;
            }

            var normalized = input.Trim();
            if (normalized.EndsWith(ClassSuffix, StringComparison.OrdinalIgnoreCase) &&
                normalized.Length > ClassSuffix.Length)
                normalized = normalized[..^ClassSuffix.Length];

            if (!ValidNameRegex.IsMatch(normalized))
            {
                ErrorMessage = "Name must be PascalCase and contain only letters and digits.";
                IsValid = false;
                ClearOutput();
                return;
            }

            _parentFolderAssetPath = normalizedParent;
            InputName = input;
            ErrorMessage = string.Empty;
            IsValid = true;

            LogicalName = normalized;
            SoClassName = $"{LogicalName}{ClassSuffix}";

            EntityFolderPath = $"{_parentFolderAssetPath}/{LogicalName}";
            ScriptFilePath = $"{EntityFolderPath}/{SoClassName}.cs";
            LeafMarkerPath = $"{EntityFolderPath}/_leaf.json";
            var firstAssetName = $"New{LogicalName}";
            FirstAssetFilePath = $"{EntityFolderPath}/{firstAssetName}.asset";
            FirstAssetAddress = $"{SoAddressConvention.AddressPrefix}{SoClassName}/{firstAssetName}";

            RefreshPreviewCache();
        }

        private static bool TryNormalizeParentFolder(string parentAssetPath, out string normalized, out string error)
        {
            normalized = parentAssetPath?.Replace('\\', '/').TrimEnd('/') ?? string.Empty;
            error = string.Empty;

            if (string.IsNullOrEmpty(normalized))
            { error = "Invalid parent folder."; return false; }

            var normalizedRoot = RootAssetPath.Replace('\\', '/').TrimEnd('/');
            if (!normalized.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            { error = $"Parent folder must be inside {RootAssetPath}."; return false; }

            EnsureFolder(normalized);
            if (!AssetDatabase.IsValidFolder(normalized))
            { error = "Invalid parent folder."; return false; }

            return true;
        }

        private void ClearOutput()
        {
            LogicalName = string.Empty;
            SoClassName = string.Empty;
            EntityFolderPath = string.Empty;
            ScriptFilePath = string.Empty;
            LeafMarkerPath = string.Empty;
            FirstAssetFilePath = string.Empty;
            FirstAssetAddress = string.Empty;

            _scriptExists = false;
            _classExists = false;
            _folderExists = false;
            _firstAssetExists = false;

            _namePreviewItems = Array.Empty<PreviewItem>();
            _pathPreviewItems = Array.Empty<PreviewItem>();
            _addressablePreviewItems = Array.Empty<PreviewItem>();
        }

        private bool ShouldCreateScript() => !_scriptExists && !_classExists;

        private PreviewStatus GetScriptStatus() =>
            string.IsNullOrEmpty(SoClassName) ? PreviewStatus.Neutral : ShouldCreateScript() ? PreviewStatus.Create : PreviewStatus.Skip;

        private PreviewStatus GetFolderStatus() =>
            string.IsNullOrEmpty(EntityFolderPath) ? PreviewStatus.Neutral : _folderExists ? PreviewStatus.Skip : PreviewStatus.Create;

        private PreviewStatus GetFirstAssetStatus() =>
            string.IsNullOrEmpty(FirstAssetFilePath) ? PreviewStatus.Neutral : _firstAssetExists ? PreviewStatus.Skip : PreviewStatus.Create;

        private void RefreshPreviewCache()
        {
            _scriptExists = FileExists(ScriptFilePath);
            _classExists = SoTypeIndex.TryFindClass(SoClassName, out _);
            _folderExists = FolderExists(EntityFolderPath);
            _firstAssetExists = FileExists(FirstAssetFilePath);

            var scriptStatus = GetScriptStatus();
            var folderStatus = GetFolderStatus();
            var assetStatus = GetFirstAssetStatus();

            _namePreviewItems = new[]
            {
                new PreviewItem("SO class", SoClassName, scriptStatus),
            };

            _pathPreviewItems = new[]
            {
                new PreviewItem("Folder",      EntityFolderPath,   folderStatus),
                new PreviewItem("Script file", ScriptFilePath,     scriptStatus),
                new PreviewItem("First asset", FirstAssetFilePath, assetStatus),
            };

            _addressablePreviewItems = new[]
            {
                new PreviewItem("Addressable group",   SoAddressConvention.GroupName, assetStatus),
                new PreviewItem("Addressable address", FirstAssetAddress,             assetStatus),
            };
        }

        private static bool FileExists(string assetPath) =>
            !string.IsNullOrEmpty(assetPath) &&
            File.Exists(Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", assetPath)));

        private static bool FolderExists(string assetPath) =>
            !string.IsNullOrEmpty(assetPath) &&
            Directory.Exists(Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", assetPath)));

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
    }

    internal readonly struct SoCreationPlan
    {
        public readonly string LogicalName;
        public readonly string SoClassName;
        public readonly string EntityFolderPath;
        public readonly string ScriptFilePath;
        public readonly string LeafMarkerPath;
        public readonly string FirstAssetFilePath;
        public readonly string FirstAssetAddress;
        public readonly bool ShouldCreateScript;

        public SoCreationPlan(
            string logicalName, string soClassName,
            string entityFolderPath, string scriptFilePath, string leafMarkerPath,
            string firstAssetFilePath, string firstAssetAddress,
            bool shouldCreateScript)
        {
            LogicalName = logicalName;
            SoClassName = soClassName;
            EntityFolderPath = entityFolderPath;
            ScriptFilePath = scriptFilePath;
            LeafMarkerPath = leafMarkerPath;
            FirstAssetFilePath = firstAssetFilePath;
            FirstAssetAddress = firstAssetAddress;
            ShouldCreateScript = shouldCreateScript;
        }
    }
}
