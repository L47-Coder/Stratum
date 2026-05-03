using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TreeView
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

        private Action<string> _onNodeSelected;
        private Action<string> _onNodeAdded;
        private Action<string> _onNodeRemoved;
        private Action<string, string> _onNodeRenamed;
        private Action<string, string> _onNodeMoved;
        private Action<int> _onButtonClicked;

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
    }
}
