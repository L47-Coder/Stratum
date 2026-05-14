using System;
using System.IO;
using System.Reflection;
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
            _treeView.ExcludePatterns = new() { "**/Generated", "**/*.asmdef", "**/*.asset" };
            _treeView.OnNodeSelect(onSelected);
        }

        public void OnGUI(Rect rect) => _treeView.Draw(rect, WorkbenchPaths.ManagerRoot);
    }

    internal sealed class ManagerCreatorPanel
    {
        private const float HPad = 6f;
        private const float VPad = 8f;

        private readonly ManagerCreatorState _state = new();
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
                    GUILayout.Space(CreatorPageDraw.SectionSpacing);
                    CreatorPageDraw.DrawPreviewCard("Addressables", _state.GetAddressablePreviewItems());
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
            CreatorPageDraw.DrawHeader("New Manager");

            var newName = CreatorPageDraw.DrawEditableField(
                "Manager name", _state.InputManagerName, _state.GetInputStatus());
            if (newName != _state.InputManagerName)
                _state.SetInputManagerName(newName);

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
                if (GUILayout.Button("Create Manager", GUILayout.Height(CreatorPageDraw.CreateButtonHeight)))
                {
                    ManagerCreationService.CreateManager(_state);
                    _state.Reset();
                    _scroll = Vector2.zero;
                    GUI.FocusControl(null);
                }
            }

            GUI.backgroundColor = prevBg;
        }
    }

    internal sealed class ManagerRightPanel
    {
        private readonly TextControl _csTextView = new();
        private readonly ManagerCreatorPanel _creatorPanel = new();
        private string _currentPath;

        private string _cachedCsPath;
        private string _cachedCsText;

        private string _cachedAssetPath;
        private IManagerConfig _cachedAsset;
        private object _cachedList;
        private MethodInfo _cachedDrawMethod;
        private TableControl _tableView;

        public void SetPath(string path)
        {
            var normalized = path?.Replace('\\', '/').TrimEnd('/') ?? string.Empty;
            _currentPath = normalized;

            if (IsBranchFolder(normalized))
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
                if (IsLeafFolder(_currentPath))
                {
                    var asset = FindAssetInFolder(_currentPath);
                    if (!string.IsNullOrEmpty(asset)) { DrawAssetFile(rect, asset); return; }
                    GUI.Label(rect, "No config asset found.", EditorStyles.centeredGreyMiniLabel);
                    return;
                }

                _creatorPanel.OnGUI(rect);
                return;
            }

            switch (Path.GetExtension(_currentPath).ToLowerInvariant())
            {
                case ".cs":    DrawCsFile(rect, _currentPath);    break;
                case ".asset": DrawAssetFile(rect, _currentPath); break;
                default:
                    GUI.Label(rect, "Unsupported file type.", EditorStyles.centeredGreyMiniLabel);
                    break;
            }
        }

        private const string LeafMarkerFileName = "_leaf.json";

        private static bool IsLeafFolder(string folderPath) =>
            Directory.Exists(folderPath) &&
            File.Exists(Path.Combine(folderPath, LeafMarkerFileName));

        private static bool IsBranchFolder(string folderPath) =>
            Directory.Exists(folderPath) && !IsLeafFolder(folderPath);

        private void DrawCsFile(Rect rect, string path)
        {
            if (_cachedCsPath != path)
            {
                _cachedCsPath = path;
                _cachedCsText = File.Exists(path) ? File.ReadAllText(path) : "(failed to read file)";
            }
            _csTextView.Draw(rect, _cachedCsText);
        }

        private static string FindAssetInFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return null;
            foreach (var f in Directory.GetFiles(folderPath, "*.asset", SearchOption.TopDirectoryOnly))
                return f.Replace('\\', '/');
            return null;
        }

        private void DrawAssetFile(Rect rect, string path)
        {
            if (_cachedAssetPath != path)
            {
                _cachedAssetPath = path;
                _cachedAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) as IManagerConfig;
                _cachedList = null;
                _cachedDrawMethod = null;
                _tableView = null;

                if (_cachedAsset != null)
                {
                    var raw = _cachedAsset.RawDataList;
                    var elemType = raw?.GetType().IsGenericType == true
                        ? raw.GetType().GetGenericArguments()[0]
                        : null;
                    if (raw != null && elemType != null)
                    {
                        _cachedList = raw;
                        _tableView = new TableControl();
                        _cachedDrawMethod = typeof(TableControl).GetMethod(nameof(TableControl.Draw))
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

            if (GUI.changed && _cachedAsset is UnityEngine.Object so) EditorUtility.SetDirty(so);
        }
    }
}
