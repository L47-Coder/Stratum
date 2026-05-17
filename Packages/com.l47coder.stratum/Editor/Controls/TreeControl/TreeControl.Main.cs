using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TreeControl
    {
        private const string LeafMarkerFileName = "_leaf.json";
        private const string RenameControlName = "TreeViewRenameField";
        private const float RowHeight = 20f;
        private const float IndentWidth = 14f;
        private const float ArrowWidth = 16f;
        private const float IconSize = 16f;
        private const float DragHoverExpandDelay = 0.35f;

        private string _searchPlaceholder = "Search...";
        private string _createLabel = "Create";
        private string _renameLabel = "Rename";
        private string _deleteLabel = "Delete";

        private string _selectedPathBacking;
        private string _cachedRootPath;
        private TreeNode _root;
        private Vector2 _scrollPos;
        private List<FlatNode> _flatList = new();
        private TreeNode _pendingContextNode;

        private Action<string> _onNodeSelect;
        private Action<string> _onNodeAdd;
        private Action<string> _onNodeRemove;
        private Action<string, string> _onNodeEdit;
        private Action<string, string> _onNodeMove;
        private Action<string> _onNodeDragOut;
        private Action<string, string> _onNodeReceiveDrop;
        private Action<int> _onButtonClick;

        private static string _projectRoot;
        private static string ProjectRoot =>
            _projectRoot ??= NormalizePath(Path.GetDirectoryName(Application.dataPath));

        private static string NormalizePath(string path) =>
            string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/').TrimEnd('/');

        private static string GetParentPath(string path) =>
            NormalizePath(Path.GetDirectoryName(NormalizePath(path)));

        private static string ToAssetPath(string path)
        {
            var n = NormalizePath(path);
            if (n.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(n, "Assets", StringComparison.OrdinalIgnoreCase)) return n;
            var root = ProjectRoot;
            return !string.IsNullOrEmpty(root) && n.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase)
                ? n[(root.Length + 1)..]
                : n;
        }

        private void DrawCore(Rect rect)
        {
            if (_pendingContextNode != null) { var n = _pendingContextNode; _pendingContextNode = null; ShowContextMenu(n); }

            CheckRenameBlur();
            HandleKeyboard();

            var normalizedPath = NormalizePath(RootPath);
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
