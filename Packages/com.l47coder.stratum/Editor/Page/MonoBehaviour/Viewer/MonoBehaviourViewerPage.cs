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
        private readonly TreeControl _treeView = new();

        public void OnFirstEnter(Action<string> onSelected)
        {
            _treeView.ExcludePatterns = new() { "**/*.asmdef", "**/*.asmref", "**/*.asset", "**/*.prefab" };
            _treeView.OnNodeSelect(onSelected);
        }

        public void OnGUI(Rect rect)
        {
            EnsureRootFolder();
            _treeView.Draw(rect, WorkbenchPaths.MonoBehaviourRoot);
        }

        private static void EnsureRootFolder()
        {
            if (AssetDatabase.IsValidFolder(WorkbenchPaths.MonoBehaviourRoot)) return;
            MonoBehaviourCreationService.EnsureFolder(WorkbenchPaths.MonoBehaviourRoot);
        }
    }

    internal sealed class MonoBehaviourCreatorPanel
    {
        private const float HPad = 6f;
        private const float VPad = 8f;

        private readonly MonoBehaviourCreatorState _state = new();
        private Vector2 _scroll;

        public void Retarget(string parentFolderAssetPath) =>
            _state.SetParentFolder(parentFolderAssetPath);

        public void OnGUI(Rect rect)
        {
            EditorGUI.DrawRect(rect, CreatorPageDraw.BgColor);

            var content = new Rect(
                rect.x + HPad, rect.y + VPad,
                rect.width - HPad * 2f, rect.height - VPad * 2f);

            GUILayout.BeginArea(content);
            var prevLW = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = CreatorPageDraw.LabelWidth;
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            try
            {
                DrawInputSection();

                if (_state.HasPreview)
                {
                    GUILayout.Space(CreatorPageDraw.SectionSpacing);
                    CreatorPageDraw.DrawPreviewCard("Type names",   _state.GetNamePreviewItems());
                    GUILayout.Space(CreatorPageDraw.SectionSpacing);
                    CreatorPageDraw.DrawPreviewCard("Output paths", _state.GetPathPreviewItems());
                    GUILayout.Space(CreatorPageDraw.SectionSpacing + 2f);
                    CreatorPageDraw.DrawLegendRow();
                }

                GUILayout.Space(CreatorPageDraw.SectionSpacing);
                DrawCreateButton();
            }
            finally
            {
                EditorGUILayout.EndScrollView();
                EditorGUIUtility.labelWidth = prevLW;
                GUILayout.EndArea();
            }
        }

        private void DrawInputSection()
        {
            CreatorPageDraw.BeginCard();
            CreatorPageDraw.DrawHeader("New MonoBehaviour");

            var newName = CreatorPageDraw.DrawEditableField(
                "Class name", _state.InputClassName, _state.GetInputStatus());
            if (newName != _state.InputClassName)
                _state.SetInputClassName(newName);

            if (!string.IsNullOrEmpty(_state.ErrorMessage))
            {
                GUILayout.Space(8f);
                EditorGUILayout.HelpBox(_state.ErrorMessage, MessageType.Warning);
            }

            CreatorPageDraw.EndCard();
        }

        private void DrawCreateButton()
        {
            var prevBg = GUI.backgroundColor;
            if (_state.IsValid) GUI.backgroundColor = CreatorPageDraw.AccentBlue;

            using (new EditorGUI.DisabledScope(!_state.IsValid))
            {
                if (GUILayout.Button("Create MonoBehaviour", GUILayout.Height(CreatorPageDraw.CreateButtonHeight)))
                {
                    MonoBehaviourCreationService.CreateScript(_state);
                    _state.Reset();
                    _scroll = Vector2.zero;
                    GUI.FocusControl(null);
                }
            }

            GUI.backgroundColor = prevBg;
        }
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
            }
            _csTextView.Draw(rect, _cachedCsText);
        }
    }
}
