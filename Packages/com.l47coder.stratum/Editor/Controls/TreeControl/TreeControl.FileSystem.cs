using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TreeControl
    {
        private enum NodeKind { Root, Branch, FolderLeaf, FileLeaf, ReadOnlyFile, ReadOnlyFolder }

        private sealed class TreeNode
        {
            public string Name;
            public string NormalizedName;
            public string FullPath;
            public NodeKind Kind;
            public List<TreeNode> Children;
            public bool IsExpanded;
            public TreeNode Parent;
        }

        private readonly struct FlatNode
        {
            public readonly TreeNode Node;
            public readonly int Depth;
            public FlatNode(TreeNode node, int depth) { Node = node; Depth = depth; }
        }

        private void RebuildTree(string rootPath)
        {
            _cachedRootPath = rootPath;
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath)) { _root = null; return; }
            _root = new TreeNode { Name = Path.GetFileName(rootPath), FullPath = rootPath, Kind = NodeKind.Root, IsExpanded = true, Children = new() };
            ScanDirectory(_root);
        }

        private void ScanDirectory(TreeNode parent)
        {
            string[] dirs, files;
            try { dirs = Directory.GetDirectories(parent.FullPath); files = Directory.GetFiles(parent.FullPath); }
            catch { return; }

            foreach (var dir in dirs)
            {
                var ap = ToAssetPath(NormalizePath(dir));
                var name = Path.GetFileName(ap);
                if (IsIgnored(name)) continue;
                var isLeaf = File.Exists($"{ap}/{LeafMarkerFileName}");
                var node = new TreeNode { Name = name, NormalizedName = NormalizeSearchFilter(name), FullPath = ap, Kind = isLeaf ? NodeKind.FolderLeaf : NodeKind.Branch, Children = new(), Parent = parent };
                if (isLeaf) ScanReadOnlyContents(node); else ScanDirectory(node);
                parent.Children.Add(node);
            }

            if (parent.Kind is NodeKind.Root or NodeKind.Branch)
                foreach (var file in files)
                {
                    var ap = ToAssetPath(NormalizePath(file));
                    var name = Path.GetFileName(ap);
                    if (!IsIgnored(name))
                        parent.Children.Add(new TreeNode { Name = name, NormalizedName = NormalizeSearchFilter(name), FullPath = ap, Kind = NodeKind.FileLeaf, Parent = parent });
                }
        }

        private void ScanReadOnlyContents(TreeNode parent)
        {
            string[] files, dirs;
            try { files = Directory.GetFiles(parent.FullPath); dirs = Directory.GetDirectories(parent.FullPath); }
            catch { return; }

            foreach (var file in files)
            {
                var ap = ToAssetPath(NormalizePath(file));
                var name = Path.GetFileName(ap);
                if (!IsIgnored(name))
                    parent.Children.Add(new TreeNode { Name = name, NormalizedName = NormalizeSearchFilter(name), FullPath = ap, Kind = NodeKind.ReadOnlyFile, Parent = parent });
            }

            foreach (var dir in dirs)
            {
                var ap = ToAssetPath(NormalizePath(dir));
                var name = Path.GetFileName(ap);
                if (IsIgnored(name)) continue;
                var sub = new TreeNode { Name = name, NormalizedName = NormalizeSearchFilter(name), FullPath = ap, Kind = NodeKind.ReadOnlyFolder, Children = new(), Parent = parent };
                ScanReadOnlyContents(sub);
                parent.Children.Add(sub);
            }
        }

        private bool IsIgnored(string name)
        {
            if (string.Equals(name, LeafMarkerFileName, StringComparison.OrdinalIgnoreCase)) return true;
            if (name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) return true;
            if (ExcludePatterns == null) return false;
            foreach (var pattern in ExcludePatterns)
            {
                if (string.IsNullOrEmpty(pattern)) continue;
                var p = pattern;
                while (p.StartsWith("**/", StringComparison.Ordinal)) p = p[3..];
                p = p.TrimEnd('/');
                if (ControlsToolbar.MatchesGlob(name, p)) return true;
            }
            return false;
        }

        private static bool CanOperateNode(TreeNode node) =>
            node != null && node.Kind is not (NodeKind.Root or NodeKind.ReadOnlyFile or NodeKind.ReadOnlyFolder);

        private static bool CanAddChildToNode(TreeNode node) =>
            node != null && node.Kind is NodeKind.Root or NodeKind.Branch;

        private static bool HasAnyLeafDescendant(TreeNode node)
        {
            if (node.Children == null) return false;
            foreach (var child in node.Children)
                if (child.Kind is NodeKind.FolderLeaf or NodeKind.FileLeaf ||
                    (child.Kind == NodeKind.Branch && HasAnyLeafDescendant(child))) return true;
            return false;
        }

        private void CreateFolderUnderContextNode(TreeNode node)
        {
            if (!CanAdd || _root == null) return;
            var target = node ?? _root;
            while (target != null && target.Kind is not (NodeKind.Root or NodeKind.Branch)) target = target.Parent;
            if (target != null) ExecuteCreateFolder(target);
        }

        private void ExecuteCreateFolder(TreeNode parent)
        {
            if (!CanAddChildToNode(parent)) return;
            var idx = 0; var name = "NewFolder";
            while (AssetDatabase.IsValidFolder($"{parent.FullPath}/{name}")) name = "NewFolder" + (++idx);
            var guid = AssetDatabase.CreateFolder(parent.FullPath, name);
            if (string.IsNullOrEmpty(guid)) { Debug.LogWarning("[TreeControl] Failed to create folder."); return; }
            var newPath = $"{parent.FullPath}/{name}";
            RefreshTree(newPath);
            _onNodeAdd?.Invoke(newPath);
        }

        private void ExecuteRename(TreeNode node, string newName)
        {
            if (!CanEdit || !CanOperateNode(node) || string.IsNullOrWhiteSpace(newName)) return;
            newName = RestoreKnownExtension(node.Name, newName.Trim());
            if (string.Equals(node.Name, newName, StringComparison.Ordinal)) return;
            var oldPath = node.FullPath;
            var destPath = $"{GetParentPath(oldPath)}/{newName}";
            var error = AssetDatabase.MoveAsset(oldPath, destPath);
            if (!string.IsNullOrEmpty(error)) { Debug.LogWarning($"[TreeControl] Rename failed: {error}"); return; }
            if (string.Equals(_selectedPathBacking, oldPath, StringComparison.OrdinalIgnoreCase)) _selectedPathBacking = destPath;
            RefreshTree(destPath);
            _onNodeEdit?.Invoke(oldPath, destPath);
        }

        private void ExecuteDelete(TreeNode node)
        {
            if (!CanOperateNode(node)) return;
            if (!EditorUtility.DisplayDialog("Confirm deletion", $"Delete \"{node.Name}\"? This operation cannot be undone.", "Delete", "Cancel")) return;
            var path = node.FullPath;
            if (!AssetDatabase.DeleteAsset(path)) { Debug.LogWarning($"[TreeControl] Delete failed: {path}"); return; }
            if (string.Equals(_selectedPathBacking, path, StringComparison.OrdinalIgnoreCase)) _selectedPathBacking = null;
            RefreshTree(_cachedRootPath);
            _onNodeRemove?.Invoke(path);
        }

        private void ExecuteMove(string sourcePath, string targetDirPath)
        {
            var src = NormalizePath(sourcePath);
            var tgt = NormalizePath(targetDirPath);
            if (string.Equals(GetParentPath(src), tgt, StringComparison.OrdinalIgnoreCase)) return;
            if (tgt.StartsWith(src + "/", StringComparison.OrdinalIgnoreCase)) { Debug.LogWarning("[TreeControl] Cannot move a folder into one of its own subfolders."); return; }
            var dest = $"{tgt}/{Path.GetFileName(src)}";
            var error = AssetDatabase.MoveAsset(src, dest);
            if (!string.IsNullOrEmpty(error)) { Debug.LogWarning($"[TreeControl] Move failed: {error}"); return; }
            if (string.Equals(_selectedPathBacking, src, StringComparison.OrdinalIgnoreCase)) _selectedPathBacking = dest;
            RefreshTree(dest);
            _onNodeMove?.Invoke(src, dest);
        }

        private void RefreshTree(string focusPath = null)
        {
            AssetDatabase.Refresh();
            var expanded = CollectExpandedPaths();
            RebuildTree(_cachedRootPath);
            RestoreExpandedPaths(expanded);
            if (!string.IsNullOrEmpty(focusPath)) _selectedPathBacking = NormalizePath(focusPath);
        }

        private HashSet<string> CollectExpandedPaths()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_root != null) CollectExpandedRec(_root, set);
            return set;
        }

        private static void CollectExpandedRec(TreeNode node, HashSet<string> set)
        {
            if (node.IsExpanded) set.Add(node.FullPath);
            if (node.Children == null) return;
            foreach (var child in node.Children) CollectExpandedRec(child, set);
        }

        private void RestoreExpandedPaths(HashSet<string> set)
        {
            if (_root != null) RestoreExpandedRec(_root, set);
        }

        private static void RestoreExpandedRec(TreeNode node, HashSet<string> set)
        {
            if (set.Contains(node.FullPath)) node.IsExpanded = true;
            if (node.Children == null) return;
            foreach (var child in node.Children) RestoreExpandedRec(child, set);
        }

        private void BuildFlatList()
        {
            _flatList.Clear();
            if (_root == null) return;
            if (!string.IsNullOrEmpty(_searchNormalized)) CollectSearchResults(_root, _flatList);
            else AppendFlatNode(_root, 0, _flatList);
        }

        private static void AppendFlatNode(TreeNode node, int depth, List<FlatNode> list)
        {
            list.Add(new FlatNode(node, depth));
            if (!node.IsExpanded || node.Children == null) return;
            foreach (var child in node.Children) AppendFlatNode(child, depth + 1, list);
        }

        private void CollectSearchResults(TreeNode node, List<FlatNode> list)
        {
            if (node.Kind == NodeKind.Root)
            {
                list.Add(new FlatNode(node, 0));
                if (node.Children == null) return;
                foreach (var child in node.Children) CollectSearchResults(child, list);
                return;
            }

            if (node.Kind is NodeKind.FolderLeaf or NodeKind.FileLeaf)
            {
                if (MatchesFuzzySearch(node.NormalizedName, _searchNormalized))
                {
                    list.Add(new FlatNode(node, 1));
                    if (node.Kind == NodeKind.FolderLeaf && node.IsExpanded && node.Children != null)
                        foreach (var child in node.Children) AppendFlatNode(child, 2, list);
                }
                return;
            }

            if (node.Children == null) return;
            foreach (var child in node.Children) CollectSearchResults(child, list);
        }

        private TreeNode FindNodeByPath(string path) =>
            _root == null || string.IsNullOrEmpty(path) ? null : FindNodeRec(_root, NormalizePath(path));

        private static TreeNode FindNodeRec(TreeNode node, string path)
        {
            if (string.Equals(node.FullPath, path, StringComparison.OrdinalIgnoreCase)) return node;
            if (node.Children == null) return null;
            foreach (var child in node.Children) { var f = FindNodeRec(child, path); if (f != null) return f; }
            return null;
        }

        internal string StripKnownExtension(string name)
        {
            if (HiddenExtensions == null) return name;
            foreach (var ext in HiddenExtensions)
                if (!string.IsNullOrEmpty(ext) && name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    return name[..^ext.Length];
            return name;
        }

        private string RestoreKnownExtension(string originalName, string newName)
        {
            if (HiddenExtensions == null) return newName;
            foreach (var ext in HiddenExtensions)
                if (!string.IsNullOrEmpty(ext) &&
                    originalName.EndsWith(ext, StringComparison.OrdinalIgnoreCase) &&
                    !newName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    return newName + ext;
            return newName;
        }
    }
}
