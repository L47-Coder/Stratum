using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Stratum.Editor
{
    internal sealed class SoCreatorState
    {
        public const string RootAssetPath = WorkbenchPaths.SoRoot;

        private static readonly Regex ValidNameRegex = new(@"^[A-Z][a-zA-Z0-9]*$", RegexOptions.Compiled);

        public string InputClassName { get; private set; } = string.Empty;
        public bool IsValid { get; private set; }
        public bool HasPreview => !string.IsNullOrEmpty(ClassName);
        public string ErrorMessage { get; private set; } = string.Empty;

        public string ClassName { get; private set; } = string.Empty;
        public string ScriptFilePath { get; private set; } = string.Empty;

        private string _parentFolderAssetPath = RootAssetPath;
        private string _existingScriptFilePath = string.Empty;
        private bool _scriptExists;

        private PreviewItem[] _namePreviewItems = Array.Empty<PreviewItem>();
        private PreviewItem[] _pathPreviewItems = Array.Empty<PreviewItem>();

        public void Reset()
        {
            InputClassName = string.Empty;
            IsValid = false;
            ErrorMessage = string.Empty;
            ClearOutput();
        }

        public void SetInputClassName(string className) => ApplyInput(className, _parentFolderAssetPath);

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
            if (string.IsNullOrWhiteSpace(InputClassName))
            {
                ClearOutput();
                ErrorMessage = string.Empty;
                IsValid = false;
                return;
            }
            ApplyInput(InputClassName, _parentFolderAssetPath);
        }

        public PreviewItem[] GetNamePreviewItems() => _namePreviewItems;
        public PreviewItem[] GetPathPreviewItems() => _pathPreviewItems;
        public PreviewStatus GetInputStatus() => IsValid ? PreviewStatus.Create : PreviewStatus.Skip;

        public SoCreationPlan BuildPlan()
        {
            RefreshExistingTargets();
            return new SoCreationPlan(
                ClassName,
                _scriptExists ? _existingScriptFilePath : ScriptFilePath,
                ShouldCreateScript());
        }

        private void ApplyInput(string className, string parentAssetPath)
        {
            if (string.IsNullOrWhiteSpace(className)) { Reset(); return; }

            InputClassName = className;
            if (!TryNormalizeParentFolder(parentAssetPath, out var normalizedParent, out var parentErr))
            {
                ErrorMessage = parentErr;
                IsValid = false;
                ClearOutput();
                return;
            }

            var normalized = className.Trim();
            if (!ValidNameRegex.IsMatch(normalized))
            {
                ErrorMessage = "Class name must be PascalCase and contain only letters and digits.";
                IsValid = false;
                ClearOutput();
                return;
            }

            _parentFolderAssetPath = normalizedParent;
            InputClassName = normalized;
            ErrorMessage = string.Empty;
            IsValid = true;

            ClassName = normalized;
            ScriptFilePath = $"{_parentFolderAssetPath}/{ClassName}.cs";

            RefreshPreviewCache();
        }

        private static bool TryNormalizeParentFolder(string parentAssetPath, out string normalized, out string error)
        {
            normalized = parentAssetPath?.Replace('\\', '/').TrimEnd('/') ?? string.Empty;
            error = string.Empty;

            if (string.IsNullOrEmpty(normalized))
            { error = "Invalid parent folder."; return false; }

            var normalizedRoot = RootAssetPath.Replace('\\', '/').TrimEnd('/');
            if (!string.Equals(normalized, normalizedRoot, StringComparison.OrdinalIgnoreCase) &&
                !normalized.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase))
            { error = $"Parent folder must be inside {RootAssetPath}."; return false; }

            EnsureFolder(normalized);
            if (!AssetDatabase.IsValidFolder(normalized))
            { error = "Invalid parent folder."; return false; }

            return true;
        }

        private void ClearOutput()
        {
            ClassName = string.Empty;
            ScriptFilePath = string.Empty;

            _existingScriptFilePath = string.Empty;
            _scriptExists = false;

            _namePreviewItems = Array.Empty<PreviewItem>();
            _pathPreviewItems = Array.Empty<PreviewItem>();
        }

        private bool ShouldCreateScript() => !_scriptExists;

        private PreviewStatus GetScriptStatus() =>
            string.IsNullOrEmpty(ClassName) ? PreviewStatus.Neutral : ShouldCreateScript() ? PreviewStatus.Create : PreviewStatus.Skip;

        private void RefreshPreviewCache()
        {
            RefreshExistingTargets();

            var scriptStatus = GetScriptStatus();

            _namePreviewItems = new[]
            {
                new PreviewItem("Class", ClassName, scriptStatus),
            };

            _pathPreviewItems = new[]
            {
                new PreviewItem("Script file", _scriptExists ? _existingScriptFilePath : ScriptFilePath, scriptStatus),
            };
        }

        private void RefreshExistingTargets()
        {
            _existingScriptFilePath = ResolveExisting(ScriptFilePath,
                SoAssetIndex.FindScript(Path.GetFileName(ScriptFilePath)));

            _scriptExists = !string.IsNullOrEmpty(_existingScriptFilePath);
        }

        private static string ResolveExisting(string preferredPath, string indexedPath)
        {
            if (FileExists(preferredPath)) return preferredPath;
            return FileExists(indexedPath) ? indexedPath : string.Empty;
        }

        private static bool FileExists(string assetPath) =>
            !string.IsNullOrEmpty(assetPath) &&
            File.Exists(Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", assetPath)));

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
        public readonly string ClassName;
        public readonly string ScriptFilePath;
        public readonly bool ShouldCreateScript;

        public SoCreationPlan(
            string className,
            string scriptFilePath,
            bool shouldCreateScript)
        {
            ClassName = className;
            ScriptFilePath = scriptFilePath;
            ShouldCreateScript = shouldCreateScript;
        }
    }
}
