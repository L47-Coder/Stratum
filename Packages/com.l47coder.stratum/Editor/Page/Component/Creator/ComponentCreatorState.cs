using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class ComponentCreatorState
    {
        public const string RootAssetPath        = WorkbenchPaths.ComponentRoot;
        public const string AddressableGroupName  = "ComponentConfig";
        public const string GeneratedFolderName   = "Generated";

        public const string SessionComponentNameKey = "ComponentCreator.ComponentName";
        public const string SessionAssetPathKey     = "ComponentCreator.AssetPath";
        public const string SessionAssetAddressKey  = "ComponentCreator.AssetAddress";

        private static readonly Regex ValidName = new(@"^[A-Z][a-zA-Z0-9]*$", RegexOptions.Compiled);

        public string InputComponentName { get; private set; } = string.Empty;
        public bool   IsValid            { get; private set; }
        public bool   HasPreview         => !string.IsNullOrEmpty(ComponentClassName);
        public string ErrorMessage       { get; private set; } = string.Empty;

        public string ComponentClassName              { get; private set; } = string.Empty;
        public string ComponentDataClassName          { get; private set; } = string.Empty;
        public string ConfigClassName                 { get; private set; } = string.Empty;
        public string ComponentFilePath               { get; private set; } = string.Empty;
        public string GeneratedFolderPath             { get; private set; } = string.Empty;
        public string GeneratedComponentPartialFilePath { get; private set; } = string.Empty;
        public string GeneratedConfigFilePath         { get; private set; } = string.Empty;
        public string GeneratedDataFilePath           { get; private set; } = string.Empty;
        public string AssetFilePath                   { get; private set; } = string.Empty;
        public string AddressableAddress              { get; private set; } = string.Empty;

        private string _parentFolder = RootAssetPath;
        private string _existingComponentFile = string.Empty;
        private string _existingAsset         = string.Empty;
        private bool   _componentFileExists;
        private bool   _componentClassExists;
        private bool   _componentDataExists;
        private bool   _generatedFolderExists;
        private bool   _assetExists;

        private PreviewItem[] _nameItems        = Array.Empty<PreviewItem>();
        private PreviewItem[] _pathItems        = Array.Empty<PreviewItem>();
        private PreviewItem[] _addressableItems = Array.Empty<PreviewItem>();

        public void Reset()
        {
            InputComponentName = string.Empty;
            IsValid            = false;
            ErrorMessage       = string.Empty;
            _parentFolder      = RootAssetPath;
            ClearDerived();
        }

        public void SetInputComponentName(string name) => Apply(name, RootAssetPath);

        public void SetInputWithParentFolder(string parentAssetPath, string name) => Apply(name, parentAssetPath);

        public void RefreshDerivedState()
        {
            if (string.IsNullOrWhiteSpace(InputComponentName))
            {
                ClearDerived();
                ErrorMessage = string.Empty;
                IsValid      = false;
                return;
            }
            Apply(InputComponentName, _parentFolder);
        }

        public PreviewItem[] GetNamePreviewItems()        => _nameItems;
        public PreviewItem[] GetPathPreviewItems()        => _pathItems;
        public PreviewItem[] GetAddressablePreviewItems() => _addressableItems;
        public PreviewStatus GetInputStatus()             => IsValid ? PreviewStatus.Create : PreviewStatus.Skip;

        public ComponentCreationPlan BuildPlan() => new(
            InputComponentName,
            ComponentClassName,
            ComponentDataClassName,
            ConfigClassName,
            Path.GetDirectoryName(ComponentFilePath)?.Replace('\\', '/'),
            ComponentFilePath,
            GeneratedFolderPath,
            GeneratedComponentPartialFilePath,
            GeneratedConfigFilePath,
            GeneratedDataFilePath,
            _assetExists ? _existingAsset : AssetFilePath,
            AddressableAddress,
            !_componentFileExists && !_componentClassExists && !_componentDataExists);

        private void Apply(string name, string parentAssetPath)
        {
            if (string.IsNullOrWhiteSpace(name)) { Reset(); return; }

            InputComponentName = name;

            var parent = (parentAssetPath ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(parent))
            {
                ErrorMessage = "Invalid parent folder.";
                IsValid      = false;
                ClearDerived();
                return;
            }

            var root = RootAssetPath.TrimEnd('/');
            if (!parent.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Parent folder must be inside the component root.";
                IsValid      = false;
                ClearDerived();
                return;
            }

            EnsureFolder(parent);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                ErrorMessage = "Invalid parent folder.";
                IsValid      = false;
                ClearDerived();
                return;
            }

            var trimmed = name.Trim();
            if (!ValidName.IsMatch(trimmed))
            {
                ErrorMessage = "Component name must follow C# naming conventions (PascalCase).";
                IsValid      = false;
                ClearDerived();
                return;
            }

            _parentFolder      = parent;
            InputComponentName = trimmed;
            ErrorMessage       = string.Empty;
            IsValid            = true;

            ComponentClassName     = $"{trimmed}Component";
            ComponentDataClassName = $"{trimmed}ComponentData";
            ConfigClassName        = $"{trimmed}ComponentConfig";

            var folder = $"{parent}/{trimmed}";
            ComponentFilePath                = $"{folder}/{ComponentClassName}.cs";
            GeneratedFolderPath              = $"{folder}/{GeneratedFolderName}";
            GeneratedComponentPartialFilePath= $"{GeneratedFolderPath}/{ComponentClassName}.Generated.cs";
            GeneratedConfigFilePath          = $"{GeneratedFolderPath}/{ConfigClassName}.cs";
            GeneratedDataFilePath            = $"{GeneratedFolderPath}/{ComponentDataClassName}.cs";
            AssetFilePath                    = $"{folder}/{ConfigClassName}.asset";
            AddressableAddress               = ComponentAddressConvention.AddressOf(trimmed);

            RefreshPreview();
        }

        private void RefreshPreview()
        {
            RefreshExisting();

            var cs    = ComponentCodeStatus();
            var cfg   = GeneratedCodeStatus();
            var asset = AssetStatus();

            _nameItems = new[]
            {
                new PreviewItem("Component class", ComponentClassName,     cs),
                new PreviewItem("Data class",      ComponentDataClassName, cs),
                new PreviewItem("Config class",    ConfigClassName,        cfg),
            };

            _pathItems = new[]
            {
                new PreviewItem("Component script", _componentFileExists ? _existingComponentFile : ComponentFilePath, cs),
                new PreviewItem("Generated folder", GeneratedFolderPath, cfg),
                new PreviewItem("Asset file",       _assetExists ? _existingAsset : AssetFilePath, asset),
            };

            _addressableItems = new[]
            {
                new PreviewItem("Addressable group",   AddressableGroupName, asset),
                new PreviewItem("Addressable address", AddressableAddress,   asset),
            };
        }

        private void RefreshExisting()
        {
            _existingComponentFile = ResolveExisting(ComponentFilePath,
                ComponentAssetIndex.FindComponentScript(Path.GetFileName(ComponentFilePath)));
            _generatedFolderExists = !string.IsNullOrEmpty(GeneratedFolderPath) && FolderExists(GeneratedFolderPath);
            _existingAsset         = ResolveExisting(AssetFilePath,
                ComponentAssetIndex.FindComponentAsset(Path.GetFileName(AssetFilePath)));

            _componentFileExists  = !string.IsNullOrEmpty(_existingComponentFile);
            _assetExists          = !string.IsNullOrEmpty(_existingAsset);
            _componentClassExists = TypeExists(ComponentClassName);
            _componentDataExists  = TypeExists(ComponentDataClassName);
        }

        private void ClearDerived()
        {
            ComponentClassName               = string.Empty;
            ComponentDataClassName           = string.Empty;
            ConfigClassName                  = string.Empty;
            ComponentFilePath                = string.Empty;
            GeneratedFolderPath              = string.Empty;
            GeneratedComponentPartialFilePath= string.Empty;
            GeneratedConfigFilePath          = string.Empty;
            GeneratedDataFilePath            = string.Empty;
            AssetFilePath                    = string.Empty;
            AddressableAddress               = string.Empty;
            _existingComponentFile           = string.Empty;
            _existingAsset                   = string.Empty;
            _componentFileExists             = false;
            _componentClassExists            = false;
            _componentDataExists             = false;
            _generatedFolderExists           = false;
            _assetExists                     = false;
            _nameItems                       = Array.Empty<PreviewItem>();
            _pathItems                       = Array.Empty<PreviewItem>();
            _addressableItems                = Array.Empty<PreviewItem>();
        }

        private PreviewStatus ComponentCodeStatus()
        {
            if (string.IsNullOrEmpty(ComponentClassName)) return PreviewStatus.Neutral;
            return !_componentFileExists && !_componentClassExists && !_componentDataExists
                ? PreviewStatus.Create : PreviewStatus.Skip;
        }

        private PreviewStatus GeneratedCodeStatus()
        {
            if (string.IsNullOrEmpty(GeneratedFolderPath)) return PreviewStatus.Neutral;
            return _generatedFolderExists ? PreviewStatus.Write : PreviewStatus.Create;
        }

        private PreviewStatus AssetStatus()
        {
            if (string.IsNullOrEmpty(AssetFilePath)) return PreviewStatus.Neutral;
            return _assetExists ? PreviewStatus.Write : PreviewStatus.Create;
        }

        private static void EnsureFolder(string assetPath)
        {
            assetPath = assetPath.Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(assetPath) || assetPath == "Assets") return;
            if (AssetDatabase.IsValidFolder(assetPath)) return;

            var parts = assetPath.Split('/');
            for (var i = 1; i < parts.Length; i++)
            {
                var parent = string.Join("/", parts, 0, i);
                var child  = $"{parent}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(child))
                    AssetDatabase.CreateFolder(parent, parts[i]);
            }
        }

        private static bool FolderExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            return Directory.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath)));
        }

        private static string ResolveExisting(string preferred, string indexed)
        {
            if (FileExists(preferred)) return preferred;
            return FileExists(indexed) ? indexed : string.Empty;
        }

        private static bool FileExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            return File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath)));
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
    }

    internal readonly struct ComponentCreationPlan
    {
        public readonly string ComponentName;
        public readonly string ComponentClassName;
        public readonly string ComponentDataClassName;
        public readonly string ConfigClassName;
        public readonly string EntityFolderPath;
        public readonly string ComponentFilePath;
        public readonly string GeneratedFolderPath;
        public readonly string GeneratedComponentPartialFilePath;
        public readonly string GeneratedConfigFilePath;
        public readonly string GeneratedDataFilePath;
        public readonly string AssetFilePath;
        public readonly string AddressableAddress;
        public readonly bool   ShouldCreateComponentFile;

        public ComponentCreationPlan(
            string componentName, string componentClassName, string componentDataClassName,
            string configClassName, string entityFolderPath, string componentFilePath,
            string generatedFolderPath, string generatedComponentPartialFilePath,
            string generatedConfigFilePath, string generatedDataFilePath,
            string assetFilePath, string addressableAddress, bool shouldCreateComponentFile)
        {
            ComponentName                    = componentName;
            ComponentClassName               = componentClassName;
            ComponentDataClassName           = componentDataClassName;
            ConfigClassName                  = configClassName;
            EntityFolderPath                 = entityFolderPath;
            ComponentFilePath                = componentFilePath;
            GeneratedFolderPath              = generatedFolderPath;
            GeneratedComponentPartialFilePath= generatedComponentPartialFilePath;
            GeneratedConfigFilePath          = generatedConfigFilePath;
            GeneratedDataFilePath            = generatedDataFilePath;
            AssetFilePath                    = assetFilePath;
            AddressableAddress               = addressableAddress;
            ShouldCreateComponentFile        = shouldCreateComponentFile;
        }
    }
}
