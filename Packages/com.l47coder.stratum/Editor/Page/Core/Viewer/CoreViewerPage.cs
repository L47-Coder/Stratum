using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class CoreViewerPage : IPage
    {
        public string GroupTitle => "Core";
        public string TabTitle => "Viewer";

        private readonly CoreLeftPanel _leftPanel = new();
        private readonly CoreRightPanel _rightPanel = new();
        private SplitterHandle _splitter = new(220f);

        public void OnFirstEnter() => _leftPanel.OnFirstEnter(_rightPanel.SetPath);
        public void OnEnter() => _leftPanel.OnEnter();

        public void OnGUI(Rect rect)
        {
            var (leftRect, rightRect) = _splitter.Draw(rect, 100f, 800f);
            _leftPanel.OnGUI(leftRect);
            _rightPanel.OnGUI(rightRect);
        }
    }

    internal sealed class CoreLeftPanel
    {
        private readonly TreeControl _treeView = new() { RootPath = WorkbenchPaths.CoreRoot };

        public void OnFirstEnter(Action<string> onSelected)
        {
            _treeView.ExcludePatterns = new() { "**/*.asmdef", "**/*.asmref", "**/*.asset", "**/*.prefab" };
            _treeView.OnNodeSelect(onSelected);
        }

        public void OnEnter()
        {
            if (string.IsNullOrEmpty(_treeView.GetSelectedPath()))
                _treeView.SelectNode(_treeView.RootPath);
        }

        public void OnGUI(Rect rect)
        {
            EnsureRootFolder();
            _treeView.Draw(rect);
        }

        private static void EnsureRootFolder()
        {
            if (AssetDatabase.IsValidFolder(WorkbenchPaths.CoreRoot)) return;
            CoreCreationService.EnsureFolder(WorkbenchPaths.CoreRoot);
        }
    }

    internal sealed class CoreCreatorPanel
    {
        private readonly CoreCreatorState _state = new();
        private readonly CreatorLayout<CoreCreatorState> _layout;

        public CoreCreatorPanel()
        {
            _layout = new CreatorLayout<CoreCreatorState>(
                _state, "Create Core",
                s => CoreCreationService.CreateScript(s));
        }

        public void Retarget(string parentFolderAssetPath) =>
            _state.SetParentFolder(parentFolderAssetPath);

        public void OnGUI(Rect rect) => _layout.OnGUI(rect);
    }

    internal sealed class CoreRightPanel
    {
        private readonly TextControl _csTextView = new();
        private readonly CoreCreatorPanel _creatorPanel = new();
        private string _currentPath;

        private string _cachedCsPath;
        private string _cachedCsText;

        public void SetPath(string path)
        {
            var normalized = path?.Replace('\\', '/').TrimEnd('/') ?? string.Empty;
            _currentPath = normalized;

            if (Directory.Exists(normalized))
                _creatorPanel.Retarget(normalized);
        }

        public void OnGUI(Rect rect)
        {
            if (string.IsNullOrEmpty(_currentPath))
            {
                GUI.Label(rect, "Nothing selected", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            if (Directory.Exists(_currentPath))
            {
                _creatorPanel.OnGUI(rect);
                return;
            }

            switch (Path.GetExtension(_currentPath).ToLowerInvariant())
            {
                case ".cs": DrawCsFile(rect, _currentPath); break;
                default:
                    GUI.Label(rect, "Unsupported file type.", EditorStyles.centeredGreyMiniLabel);
                    break;
            }
        }

        private void DrawCsFile(Rect rect, string path)
        {
            if (_cachedCsPath != path)
            {
                _cachedCsPath = path;
                _cachedCsText = File.Exists(path) ? File.ReadAllText(path) : "(failed to read file)";
                _csTextView.Text = _cachedCsText;
            }
            _csTextView.Draw(rect);
        }
    }
}
