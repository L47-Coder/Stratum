using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class MonoBehaviourViewerPage : IPage
    {
        public string GroupTitle => "MonoBehaviour";
        public string TabTitle => "Viewer";

        private readonly MonoBehaviourLeftPanel _leftPanel = new();
        private readonly MonoBehaviourRightPanel _rightPanel = new();
        private SplitterHandle _splitter = new(220f);

        public void OnFirstEnter() => _leftPanel.OnFirstEnter(_rightPanel.SetPath);

        public void OnGUI(Rect rect)
        {
            var (leftRect, rightRect) = _splitter.Draw(rect, 100f, 800f);
            _leftPanel.OnGUI(leftRect);
            _rightPanel.OnGUI(rightRect);
        }
    }

    internal sealed class MonoBehaviourLeftPanel
    {
        private readonly TreeControl _treeView = new() { RootPath = WorkbenchPaths.MonoBehaviourRoot };

        public void OnFirstEnter(Action<string> onSelected)
        {
            _treeView.ExcludePatterns = new() { "**/*.asmdef", "**/*.asmref", "**/*.asset", "**/*.prefab" };
            _treeView.OnNodeSelect(onSelected);
        }

        public void OnGUI(Rect rect)
        {
            EnsureRootFolder();
            _treeView.Draw(rect);
        }

        private static void EnsureRootFolder()
        {
            if (AssetDatabase.IsValidFolder(WorkbenchPaths.MonoBehaviourRoot)) return;
            MonoBehaviourCreationService.EnsureFolder(WorkbenchPaths.MonoBehaviourRoot);
        }
    }

    internal sealed class MonoBehaviourCreatorPanel
    {
        private readonly MonoBehaviourCreatorState _state = new();
        private readonly CreatorLayout<MonoBehaviourCreatorState> _layout;

        public MonoBehaviourCreatorPanel()
        {
            _layout = new CreatorLayout<MonoBehaviourCreatorState>(
                _state, "Create MonoBehaviour",
                s => MonoBehaviourCreationService.CreateScript(s));
        }

        public void Retarget(string parentFolderAssetPath) =>
            _state.SetParentFolder(parentFolderAssetPath);

        public void OnGUI(Rect rect) => _layout.OnGUI(rect);
    }

    internal sealed class MonoBehaviourRightPanel
    {
        private readonly TextControl _csTextView = new();
        private readonly MonoBehaviourCreatorPanel _creatorPanel = new();
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
