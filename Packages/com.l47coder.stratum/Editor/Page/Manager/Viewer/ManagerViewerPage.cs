using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class ManagerViewerPage : IPage
    {
        public string GroupTitle => "Manager";
        public string TabTitle => "Viewer";

        private readonly ManagerLeftPanel _leftPanel = new();
        private readonly ManagerRightPanel _rightPanel = new();
        private SplitterHandle _splitter = new(220f);

        public void OnFirstEnter() => _leftPanel.OnFirstEnter(_rightPanel.SetPath);

        public void OnGUI(Rect rect)
        {
            var (leftRect, rightRect) = _splitter.Draw(rect, 100f, 800f);
            _leftPanel.OnGUI(leftRect);
            _rightPanel.OnGUI(rightRect);
        }
    }

    internal sealed class ManagerLeftPanel
    {
        private readonly TreeControl _treeView = new();

        public void OnFirstEnter(Action<string> onSelected)
        {
            _treeView.ExcludePatterns = new() { "**/*.asmdef", "**/*.asmref", "**/*.InternalsVisibleTo.cs", "**/*.asset" };
            _treeView.OnNodeSelect(onSelected);
        }

        public void OnGUI(Rect rect) => _treeView.Draw(rect, WorkbenchPaths.ManagerRoot);
    }

    internal sealed class ManagerCreatorPanel
    {
        private readonly ManagerCreatorState _state = new();
        private readonly CreatorLayout<ManagerCreatorState> _layout;

        public ManagerCreatorPanel()
        {
            _layout = new CreatorLayout<ManagerCreatorState>(
                _state, "Create Manager",
                s => ManagerCreationService.CreateManager(s));
        }

        public void Retarget(string parentFolderAssetPath) =>
            _state.SetParentFolder(parentFolderAssetPath);

        public void OnGUI(Rect rect) => _layout.OnGUI(rect);
    }

    internal sealed class ManagerRightPanel
    {
        private readonly TextControl _csTextView = new();
        private readonly ManagerCreatorPanel _creatorPanel = new();
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
            }
            _csTextView.Draw(rect, _cachedCsText);
        }
    }
}
