using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class ListView
    {
        public bool CanAdd { get; set; } = true;
        public bool CanRemove { get; set; } = true;
        public bool CanSelect { get; set; } = true;
        public bool CanDrag { get; set; } = true;
        public bool CanRename { get; set; } = true;
        public bool ShowToolbar { get; set; } = true;
        public bool CanReceiveDrop { get; set; }
        public List<string> ExcludePatterns { get; set; } = new();
        public List<GUIContent> ToolbarButtons { get; set; } = new();

        public void OnRowAdded(Action<int> callback) => _onRowAdded = callback;
        public void OnRowRemoved(Action<int> callback) => _onRowRemoved = callback;
        public void OnRowSelected(Action<int> callback) => _onRowSelected = callback;
        public void OnRowMoved(Action<int, int> callback) => _onRowMoved = callback;
        public void OnRowRenamed(Action<int> callback) => _onRowRenamed = callback;
        public void OnDropOnRow(Action<int> callback) => _onDropOnRow = callback;
        public void OnButtonClicked(Action<int> callback) => _onButtonClicked = callback;

        public void Draw(Rect rect, List<string> items)
        {
            CheckRenameBlur();
            HandleKeyboard();
            if (items == null) return;

            if (_pendingBodyContextMenu) { _pendingBodyContextMenu = false; ShowBodyContextMenu(items); }
            if (_hasPendingContextMenu) { _hasPendingContextMenu = false; ShowContextMenu(items, _pendingContextIndex, _pendingContextLabel); _pendingContextLabel = null; }

            if (_pendingBeginRename && _selectedIndex >= 0 && _selectedIndex < items.Count)
            { BeginRename(items, _selectedIndex, items[_selectedIndex]); _pendingBeginRename = false; }

            if (_pendingDelete && _selectedIndex >= 0 && _selectedIndex < items.Count)
            { TryRemoveRowAt(items, _selectedIndex); _pendingDelete = false; }

            var boxRect = BoxDrawer.CalcBoxRect(rect);
            if (boxRect.width < 1f || boxRect.height < 1f) return;

            BoxDrawer.DrawBox(boxRect);
            var contentRect = BoxDrawer.CalcContentRect(boxRect);
            GUI.BeginGroup(contentRect);

            var tbH = ShowToolbar ? ControlsToolbar.ToolbarHeight : 0f;
            if (ShowToolbar) DrawToolbar(new Rect(0f, 0f, contentRect.width, tbH));
            else _searchText = string.Empty;

            if (_selectedIndex >= items.Count) { _selectedIndex = items.Count - 1; GUI.changed = true; }
            if (_renamingIndex >= 0 && _renamingIndex >= items.Count) CommitRename(false);

            DrawBody(new Rect(0f, tbH, contentRect.width, Mathf.Max(0f, contentRect.height - tbH)), items, GetFilteredIndices(items));
            GUI.EndGroup();
        }
    }
}
