using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class MonoBehaviourCreatorState
    {
        public const string RootAssetPath = WorkbenchPaths.MonoBehaviourRoot;

        private static readonly Regex ValidClassNameRegex = new(@"^[A-Z][a-zA-Z0-9]*$", RegexOptions.Compiled);

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

        public MonoBehaviourCreationPlan BuildPlan()
        {
            RefreshExistingTargets();
            return new MonoBehaviourCreationPlan(
                ClassName,
                _scriptExists ? _existingScriptFilePath : ScriptFilePath,
                ShouldCreateScript());
        }

        private void ApplyInput(string className, string parentAssetPath)
        {
            if (string.IsNullOrWhiteSpace(className)) { Reset(); return; }

            InputClassName = className;
            if (!TryNormalizeParentFolder(parentAssetPath, out var normalizedParent, out var parentError))
            {
                ErrorMessage = parentError;
                IsValid = false;
                ClearOutput();
                return;
            }

            var normalizedName = className.Trim();
            if (!ValidClassNameRegex.IsMatch(normalizedName))
            {
                ErrorMessage = "Class name must be PascalCase and contain only letters and digits.";
                IsValid = false;
                ClearOutput();
                return;
            }

            _parentFolderAssetPath = normalizedParent;
            InputClassName = normalizedName;
            ErrorMessage = string.Empty;
            IsValid = true;

            ClassName = normalizedName;
            ScriptFilePath = $"{_parentFolderAssetPath}/{ClassName}.cs";

            RefreshPreviewCache();
        }

        private static bool TryNormalizeParentFolder(string parentAssetPath, out string normalizedParentPath, out string errorMessage)
        {
            normalizedParentPath = parentAssetPath?.Replace('\\', '/').TrimEnd('/') ?? string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(normalizedParentPath))
            { errorMessage = "Invalid parent folder."; return false; }

            var normalizedRoot = RootAssetPath.Replace('\\', '/').TrimEnd('/');
            if (!string.Equals(normalizedParentPath, normalizedRoot, StringComparison.OrdinalIgnoreCase) &&
                !normalizedParentPath.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase))
            { errorMessage = "Parent folder must be inside the MonoBehaviour root."; return false; }

            EnsureFolder(normalizedParentPath);
            if (!AssetDatabase.IsValidFolder(normalizedParentPath))
            { errorMessage = "Invalid parent folder."; return false; }

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

        private static PreviewStatus GetFileStatus(string path, bool exists) =>
            string.IsNullOrEmpty(path) ? PreviewStatus.Neutral : exists ? PreviewStatus.Skip : PreviewStatus.Create;

        private void RefreshPreviewCache()
        {
            RefreshExistingTargets();

            var scriptStatus = GetFileStatus(ScriptFilePath, _scriptExists);

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
                MonoBehaviourAssetIndex.FindScript(Path.GetFileName(ScriptFilePath)));

            _scriptExists = !string.IsNullOrEmpty(_existingScriptFilePath);
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
    }

    internal readonly struct MonoBehaviourCreationPlan
    {
        public readonly string ClassName;
        public readonly string ScriptFilePath;
        public readonly bool ShouldCreateScript;

        public MonoBehaviourCreationPlan(
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
