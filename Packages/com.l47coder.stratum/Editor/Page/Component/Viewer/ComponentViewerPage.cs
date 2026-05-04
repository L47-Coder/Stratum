using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class ComponentViewerPage : IPage
    {
        public string GroupTitle => "Component";
        public string TabTitle   => "Viewer";

        private readonly ComponentLeftPanel  _leftPanel  = new();
        private readonly ComponentRightPanel _rightPanel = new();
        private SplitterHandle _splitter = new(220f);

        public void OnFirstEnter() => _leftPanel.OnFirstEnter(_rightPanel.SetPath);

        public void OnGUI(Rect rect)
        {
            var (leftRect, rightRect) = _splitter.Draw(rect, 100f, 800f);
            _leftPanel.OnGUI(leftRect);
            _rightPanel.OnGUI(rightRect);
        }
    }

    internal sealed class ComponentLeftPanel
    {
        private readonly TreeView _treeView = new();

        public void OnFirstEnter(Action<string> onSelected)
        {
            _treeView.ExcludePatterns = new() { "**/Generated", "**/Editor", "**/*.InternalsVisibleTo.cs", "**/*.asmdef" };
            _treeView.HiddenExtensions = new() { ".cs", ".asset" };
            _treeView.OnNodeSelected(onSelected);
        }

        public void OnGUI(Rect rect) => _treeView.Draw(rect, WorkbenchPaths.ComponentRoot);
    }

    internal sealed class ComponentRightPanel
    {
        private readonly TextView _csTextView = new();
        private string _currentPath;

        private string _cachedCsPath;
        private string _cachedCsText;

        private string _cachedAssetPath;
        private BaseComponentConfig _cachedAsset;
        private object _cachedList;
        private MethodInfo _cachedDrawMethod;
        private TableView _tableView;

        public void SetPath(string path) => _currentPath = path;

        public void OnGUI(Rect rect)
        {
            if (string.IsNullOrEmpty(_currentPath))
            {
                GUI.Label(rect, "Nothing selected", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            if (Directory.Exists(_currentPath)) { DrawFolder(rect, _currentPath); return; }

            switch (Path.GetExtension(_currentPath).ToLowerInvariant())
            {
                case ".cs":    DrawCsFile(rect, _currentPath);    break;
                case ".asset": DrawAssetFile(rect, _currentPath); break;
                default:
                    GUI.Label(rect, "Unsupported file type.", EditorStyles.centeredGreyMiniLabel);
                    break;
            }
        }

        private static void DrawFolder(Rect rect, string path)
        {
            var inner = new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f);
            GUI.Label(inner, path, EditorStyles.wordWrappedLabel);
        }

        private void DrawCsFile(Rect rect, string path)
        {
            if (_cachedCsPath != path)
            {
                _cachedCsPath = path;
                _cachedCsText = File.Exists(path) ? File.ReadAllText(path) : "(failed to read file)";
            }
            _csTextView.Draw(rect, _cachedCsText);
        }

        private void DrawAssetFile(Rect rect, string path)
        {
            if (_cachedAssetPath != path)
            {
                _cachedAssetPath  = path;
                _cachedAsset      = AssetDatabase.LoadAssetAtPath<BaseComponentConfig>(path);
                _cachedList       = null;
                _cachedDrawMethod = null;
                _tableView        = null;

                if (_cachedAsset != null)
                {
                    _cachedList = _cachedAsset.GetConfigList();
                    var elemType = _cachedAsset.ConfigItemType;
                    if (_cachedList != null && elemType != null)
                    {
                        _tableView        = new TableView();
                        _cachedDrawMethod = typeof(TableView).GetMethod(nameof(TableView.Draw))
                            .MakeGenericMethod(elemType);
                    }
                }
            }

            if (_cachedAsset == null || _cachedList == null || _cachedDrawMethod == null)
            {
                GUI.Label(rect, "Failed to read the config list.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            _cachedDrawMethod.Invoke(_tableView, new object[] { rect, _cachedList });

            if (GUI.changed) EditorUtility.SetDirty(_cachedAsset);
        }
    }
}
