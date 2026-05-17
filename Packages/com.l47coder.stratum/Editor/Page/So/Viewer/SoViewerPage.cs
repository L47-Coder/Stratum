using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class SoViewerPage : IPage
    {
        public string GroupTitle => "ScriptableObject";
        public string TabTitle => "Viewer";

        private readonly SoLeftPanel _leftPanel = new();
        private readonly SoRightPanel _rightPanel = new();
        private SplitterHandle _splitter = new(220f);

        public void OnFirstEnter() => _leftPanel.OnFirstEnter(_rightPanel.SetPath);

        public void OnGUI(Rect rect)
        {
            var (leftRect, rightRect) = _splitter.Draw(rect, 100f, 800f);
            _leftPanel.OnGUI(leftRect);
            _rightPanel.OnGUI(rightRect);
        }
    }

    internal sealed class SoLeftPanel
    {
        private readonly TreeControl _treeView = new();

        public void OnFirstEnter(Action<string> onSelected)
        {
            _treeView.ExcludePatterns = new() { "**/*.asmdef", "**/*.asset" };
            _treeView.OnNodeSelect(onSelected);
        }

        public void OnGUI(Rect rect)
        {
            EnsureRootFolder();
            _treeView.Draw(rect, WorkbenchPaths.SoRoot);
        }

        private static void EnsureRootFolder()
        {
            if (AssetDatabase.IsValidFolder(WorkbenchPaths.SoRoot)) return;
            SoCreationService.EnsureFolder(WorkbenchPaths.SoRoot);
        }
    }

    internal sealed class SoRightPanel
    {
        private readonly TextControl _csTextView = new();
        private readonly SoCreatorPanel _creatorPanel = new();
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
