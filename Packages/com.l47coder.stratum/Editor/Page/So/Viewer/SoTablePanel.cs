using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class SoTablePanel
    {
        private string _leafFolder;
        private Type _soType;
        private List<SoRow> _rows = new();
        private readonly TableControl _table = new();
        private bool _wired;

        public void Retarget(string leafFolderPath)
        {
            _leafFolder = leafFolderPath?.Replace('\\', '/').TrimEnd('/') ?? string.Empty;
            _soType = SoTypeIndex.ResolveType(_leafFolder);
            EnsureWired();
            Rescan();
        }

        public void OnGUI(Rect rect)
        {
            if (string.IsNullOrEmpty(_leafFolder))
            {
                GUI.Label(rect, "No folder selected.", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            if (_soType == null)
            {
                GUI.Label(rect, $"No SO type found in {_leafFolder}.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            _table.Draw(rect, _rows);
        }

        private void EnsureWired()
        {
            if (_wired) return;
            _wired = true;

            EditorApplication.projectChanged += OnProjectChanged;

            _table.CanAdd = true;
            _table.CanRemove = true;
            _table.CanReorder = false;
            _table.CanDragOut = false;
            _table.CanReceiveDrop = false;
            _table.KeyField = nameof(SoRow.Name);

            _table.OnRowAdd(addedIndex =>
            {
                if (_soType == null || string.IsNullOrEmpty(_leafFolder)) return;
                if (!SoCreationService.CreateNewAsset(_soType, _leafFolder, out _)) return;
                EditorApplication.delayCall += Rescan;
            });

            _table.OnRowRemove(i =>
            {
                if (i < 0 || i >= _rows.Count) return;
                var row = _rows[i];
                if (row?.AssetPath == null) return;

                var confirmed = EditorUtility.DisplayDialog(
                    "Delete ScriptableObject",
                    $"Delete this asset?\n\n{row.AssetPath}\n\nThis will also remove its Addressable entry.",
                    "Delete", "Cancel");

                if (!confirmed)
                {
                    EditorApplication.delayCall += Rescan;
                    return;
                }

                SoDetailWindow.CloseAssetByPath(row.AssetPath);
                SoCreationService.DeleteAsset(row.AssetPath);
                EditorApplication.delayCall += Rescan;
            });

            _table.OnRowSelect(i =>
            {
                if (i < 0 || i >= _rows.Count) return;
                var target = _rows[i]?.Target;
                if (target != null) SoDetailWindow.OpenAsset(target);
            });
        }

        private void Rescan()
        {
            _rows.Clear();
            if (_soType == null || string.IsNullOrEmpty(_leafFolder)) return;
            _rows.AddRange(SoAssetScanner.Scan(_leafFolder, _soType));
            DevWindow.Refresh();
        }

        private void OnProjectChanged()
        {
            if (string.IsNullOrEmpty(_leafFolder) || _soType == null) return;
            Rescan();
        }
    }
}
