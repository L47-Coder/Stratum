using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class PrefabCreatorState
    {
        public const string AddressableGroupName = "Prefab";

        private static readonly Regex ValidName =
            new(@"^[A-Za-z_][A-Za-z0-9 _\-]*$", RegexOptions.Compiled);

        public string InputPrefabName { get; private set; } = string.Empty;
        public bool IsValid { get; private set; }
        public bool HasPreview => !string.IsNullOrEmpty(PrefabFilePath);
        public string ErrorMessage { get; private set; } = string.Empty;

        public string PrefabFilePath { get; private set; } = string.Empty;
        public string AddressableAddress { get; private set; } = string.Empty;

        private PreviewItem[] _pathItems = Array.Empty<PreviewItem>();
        private PreviewItem[] _addressableItems = Array.Empty<PreviewItem>();

        public void Reset()
        {
            InputPrefabName = string.Empty;
            IsValid = false;
            ErrorMessage = string.Empty;
            ClearDerived();
        }

        public void SetInputPrefabName(string name) => Apply(name);

        public void RefreshDerivedState()
        {
            if (string.IsNullOrWhiteSpace(InputPrefabName))
            {
                ClearDerived();
                ErrorMessage = string.Empty;
                IsValid = false;
                return;
            }
            Apply(InputPrefabName);
        }

        public PreviewItem[] GetPathPreviewItems() => _pathItems;
        public PreviewItem[] GetAddressablePreviewItems() => _addressableItems;
        public PreviewStatus GetInputStatus() => IsValid ? PreviewStatus.Create : PreviewStatus.Skip;

        private void Apply(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { Reset(); return; }

            var trimmed = name.Trim();

            if (!ValidName.IsMatch(trimmed))
            {
                InputPrefabName = trimmed;
                ErrorMessage = "Prefab name contains invalid characters.";
                IsValid = false;
                ClearDerived();
                return;
            }

            var filePath = $"{WorkbenchPaths.PrefabRoot}/{trimmed}.prefab";

            if (FileExists(filePath))
            {
                InputPrefabName = trimmed;
                ErrorMessage = "A prefab with this name already exists.";
                IsValid = false;
                PrefabFilePath = filePath;
                AddressableAddress = PrefabAddressConvention.AddressOf(trimmed);
                RefreshPreview(PreviewStatus.Skip);
                return;
            }

            InputPrefabName = trimmed;
            ErrorMessage = string.Empty;
            IsValid = true;
            PrefabFilePath = filePath;
            AddressableAddress = PrefabAddressConvention.AddressOf(trimmed);
            RefreshPreview(PreviewStatus.Create);
        }

        private void RefreshPreview(PreviewStatus status)
        {
            _pathItems = new[]
            {
                new PreviewItem("Prefab file", PrefabFilePath, status),
            };

            _addressableItems = new[]
            {
                new PreviewItem("Addressable group",   AddressableGroupName,  status),
                new PreviewItem("Addressable address", AddressableAddress,    status),
            };
        }

        private void ClearDerived()
        {
            PrefabFilePath = string.Empty;
            AddressableAddress = string.Empty;
            _pathItems = Array.Empty<PreviewItem>();
            _addressableItems = Array.Empty<PreviewItem>();
        }

        private static bool FileExists(string assetPath) =>
            !string.IsNullOrEmpty(assetPath) && File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath)));
    }
}
