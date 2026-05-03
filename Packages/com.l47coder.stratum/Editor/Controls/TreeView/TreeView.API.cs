using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TreeView
    {
        public bool CanAdd { get; set; } = true;
        public bool CanRemove { get; set; } = true;
        public bool CanSelect { get; set; } = true;
        public bool CanDrag { get; set; } = true;
        public bool CanRename { get; set; } = true;
        public bool ShowToolbar { get; set; } = true;
        public List<GUIContent> ToolbarButtons { get; set; } = new();
        public List<string> ExcludePatterns { get; set; } = new();
        public List<string> HiddenExtensions { get; set; }

        public void OnNodeAdded(Action<string> callback) => _onNodeAdded = callback;
        public void OnNodeRemoved(Action<string> callback) => _onNodeRemoved = callback;
        public void OnNodeSelected(Action<string> callback) => _onNodeSelected = callback;
        public void OnNodeMoved(Action<string, string> callback) => _onNodeMoved = callback;
        public void OnNodeRenamed(Action<string, string> callback) => _onNodeRenamed = callback;
        public void OnButtonClicked(Action<int> callback) => _onButtonClicked = callback;

        public void Draw(Rect rect, string path)
        {
            if (_pendingContextNode != null) { var n = _pendingContextNode; _pendingContextNode = null; ShowContextMenu(n); }

            CheckRenameBlur();
            HandleKeyboard();

            var normalizedPath = NormalizePath(path);
            if (_root == null || normalizedPath != _cachedRootPath) RebuildTree(normalizedPath);

            var box = BoxDrawer.CalcBoxRect(rect);
            if (box.width < 1f || box.height < 1f) return;
            BoxDrawer.DrawBox(box);

            var content = BoxDrawer.CalcContentRect(box);
            GUI.BeginGroup(content);

            var tbH = ShowToolbar ? ControlsToolbar.ToolbarHeight : 0f;
            if (ShowToolbar) DrawToolbar(new Rect(0f, 0f, content.width, tbH));
            else { _searchText = string.Empty; _searchNormalized = string.Empty; }

            BuildFlatList();
            var body = new Rect(0f, tbH, content.width, Mathf.Max(0f, content.height - tbH));
            HandleGlobalDragEvents(body, _flatList);
            DrawBody(body, _flatList);

            GUI.EndGroup();
        }
    }
}
