using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class PrefabViewerPage : IPage
    {
        public string GroupTitle => "Prefab";
        public string TabTitle => "Viewer";

        private const float SplitterVisualW = 1f;
        private const float LeftPanelMin = 100f;
        private const float LeftPanelMax = 800f;
        private const float LeftPanelStart = 220f;
        private static readonly Color SplitterColor = new(0.11f, 0.11f, 0.11f);

        private readonly PrefabViewerLeftPanel _leftPanel = new();
        private readonly PrefabViewerRightPanel _rightPanel = new();
        private float _splitterX = LeftPanelStart;
        private bool _dragging;

        public void OnFirstEnter() => _leftPanel.OnFirstEnter(_rightPanel.SetPath);

        public void OnEnter() => _leftPanel.OnEnter();

        public void OnGUI(Rect rect)
        {
            var visualRect = new Rect(rect.x + _splitterX, rect.y, SplitterVisualW, rect.height);
            var hitRect = new Rect(rect.x + _splitterX - 2f, rect.y, SplitterVisualW + 4f, rect.height);

            EditorGUIUtility.AddCursorRect(hitRect, MouseCursor.ResizeHorizontal);

            var evt = Event.current;
            switch (evt.type)
            {
                case EventType.MouseDown when hitRect.Contains(evt.mousePosition):
                    _dragging = true;
                    evt.Use();
                    break;
                case EventType.MouseDrag when _dragging:
                    var maxX = Mathf.Min(LeftPanelMax, rect.width - LeftPanelMin - SplitterVisualW);
                    _splitterX = Mathf.Clamp(evt.mousePosition.x - rect.x, LeftPanelMin, maxX);
                    evt.Use();
                    break;
                case EventType.MouseUp when _dragging:
                    _dragging = false;
                    evt.Use();
                    break;
            }

            var leftRect = new Rect(rect.x, rect.y, _splitterX, rect.height);
            var rightRect = new Rect(visualRect.xMax, rect.y,
                                     rect.width - _splitterX - SplitterVisualW, rect.height);

            _leftPanel.OnGUI(leftRect);
            EditorGUI.DrawRect(visualRect, SplitterColor);
            _rightPanel.OnGUI(rightRect);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Left Panel
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class PrefabViewerLeftPanel
    {
        private readonly TreeControl _treeView = new();

        public void OnFirstEnter(Action<string> onSelected)
        {
            _treeView.HiddenExtensions = new() { ".prefab" };
            _treeView.CanAdd = true;
            _treeView.OnNodeSelect(onSelected);
        }

        public void OnEnter() => _treeView.RebuildTree();

        public void OnGUI(Rect rect) =>
            _treeView.Draw(rect, WorkbenchPaths.PrefabRoot);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Right Panel
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class PrefabViewerRightPanel
    {
        private const float Padding = 8f;
        private const float RowH = 20f;

        private static GUIStyle _keyLabelStyle;
        private static GUIStyle _hintLabelStyle;
        private static GUIStyle _hintLabelCenterStyle;

        private static GUIStyle KeyLabelStyle =>
            _keyLabelStyle ??= new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.58f, 0.58f, 0.58f) } };

        private static GUIStyle HintLabelStyle =>
            _hintLabelStyle ??= new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.65f, 0.65f, 0.65f) } };

        private static GUIStyle HintLabelCenterStyle =>
            _hintLabelCenterStyle ??= new GUIStyle(EditorStyles.miniLabel)
            { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.65f, 0.65f, 0.65f) } };

        private static readonly Color AddrLinkRowBg = new(0.115f, 0.115f, 0.12f);
        private static readonly Color AddrLinkRowBgHover = new(0.145f, 0.15f, 0.168f);
        private string _currentPath;
        private string _cachedPath;
        private GameObject _cachedPrefab;
        private Entity _cachedEntity;
        private SerializedObject _cachedSo;

        private bool _isAddressable;
        private string _addrAddress;
        private string _addrGroup;
        private string _addrLabels;

        private readonly TableControl _tableView   = new();
        private readonly FieldPopup   _configPopup = new();
        private bool _tableSetup;

        // ── Public API ───────────────────────────────────────────────────────

        public void SetPath(string path)
        {
            if (_currentPath == path) return;
            _currentPath = path;
            _cachedPath = null;
        }

        // ── Draw entry ───────────────────────────────────────────────────────

        public void OnGUI(Rect rect)
        {
            if (string.IsNullOrEmpty(_currentPath))
            {
                GUI.Label(rect, "Nothing selected", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            if (Directory.Exists(_currentPath))
            {
                var inner = new Rect(rect.x + Padding, rect.y + Padding,
                                     rect.width - Padding * 2, rect.height - Padding * 2);
                GUI.Label(inner, _currentPath, EditorStyles.wordWrappedLabel);
                return;
            }

            if (!_currentPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                GUI.Label(rect, "Select a prefab file.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            RefreshCacheIfNeeded();

            if (_cachedPrefab == null)
            {
                GUI.Label(rect, "Failed to load prefab.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            DrawPanel(rect);
        }

        // ── Cache ─────────────────────────────────────────────────────────────

        private void RefreshCacheIfNeeded()
        {
            if (_cachedPath == _currentPath) return;

            _cachedPath = _currentPath;
            _cachedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(_currentPath);
            _cachedEntity = _cachedPrefab != null ? _cachedPrefab.GetComponent<Entity>() : null;
            _cachedSo = _cachedEntity != null ? new SerializedObject(_cachedEntity) : null;

            RefreshAddressableInfo();
        }

        private void RefreshAddressableInfo()
        {
            _isAddressable = false;
            _addrAddress = string.Empty;
            _addrGroup = string.Empty;
            _addrLabels = string.Empty;

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            var guid = AssetDatabase.AssetPathToGUID(_currentPath);
            var entry = settings.FindAssetEntry(guid);
            if (entry == null) return;

            _isAddressable = true;
            _addrAddress = entry.address;
            _addrGroup = entry.parentGroup?.Name ?? string.Empty;
            _addrLabels = string.Join(", ", entry.labels);
        }

        // ── Panel layout ──────────────────────────────────────────────────────

        private void DrawPanel(Rect rect)
        {
            var addrHeaderH = CalcAddressHeaderHeight();

            var x = rect.x;
            var w = rect.width;
            var y = rect.y;

            DrawAddressableHeader(x, y, w, addrHeaderH);
            y += addrHeaderH;

            DrawEntityConfigPanel(x, y, w, rect.yMax);
        }

        private float CalcAddressHeaderHeight()
        {
            return RowH;
        }

        // ── Addressable header ────────────────────────────────────────────────

        private void DrawAddressableHeader(float x, float y, float w, float h)
        {
            if (!_isAddressable)
                DrawMarkAddressableHintBlock(ref y, x, w);
            else
                DrawAddressWorkbenchLinkRow(ref y, x, w);
        }

        private void DrawMarkAddressableHintBlock(ref float y, float x, float w)
        {
            var blockRect = new Rect(x, y, w, RowH);
            var hover = blockRect.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(blockRect, hover ? AddrLinkRowBgHover : AddrLinkRowBg);

            EditorGUIUtility.AddCursorRect(blockRect, MouseCursor.Link);

            if (Event.current.type == EventType.MouseDown && hover && Event.current.button == 0)
            {
                MarkAsAddressable();
                Event.current.Use();
            }

            EditorGUI.LabelField(new Rect(x, y, w, RowH), "尚未加入 Addressable 组，点击标记", HintLabelCenterStyle);
            y += RowH;
        }

        private void DrawAddressWorkbenchLinkRow(ref float y, float x, float w)
        {
            var blockRect = new Rect(x, y, w, RowH);
            var hover = blockRect.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(blockRect, hover ? AddrLinkRowBgHover : AddrLinkRowBg);

            EditorGUIUtility.AddCursorRect(blockRect, MouseCursor.Link);

            if (Event.current.type == EventType.MouseDown && hover && Event.current.button == 0)
            {
                AddressableViewerPage.NavigateFromPrefab(_addrGroup, _cachedPath);
                Event.current.Use();
            }

            const float keyW = 56f;
            var contentX = x + Padding;
            var contentW = w - Padding * 2f;

            EditorGUI.LabelField(new Rect(contentX, y, keyW, RowH), "Address", KeyLabelStyle);
            EditorGUI.LabelField(new Rect(contentX + keyW, y, contentW - keyW, RowH), _addrAddress, EditorStyles.miniLabel);
            y += RowH;
        }

        // ── Entity / EntityComponentEntry 表格 ────────────────────────────────

        private void DrawEntityConfigPanel(float x, float y, float w, float yMax)
        {
            if (_cachedEntity == null)
            {
                var pad = Padding;
                var blockRect = new Rect(x + pad, y + pad, w - pad * 2f, RowH);
                var hover = blockRect.Contains(Event.current.mousePosition);

                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(blockRect, hover ? AddrLinkRowBgHover : AddrLinkRowBg);

                EditorGUIUtility.AddCursorRect(blockRect, MouseCursor.Link);

                if (Event.current.type == EventType.MouseDown && hover && Event.current.button == 0)
                {
                    AddEntity();
                    Event.current.Use();
                }

                EditorGUI.LabelField(blockRect, "此预制体尚未挂载 Entity 组件，点击挂载", HintLabelCenterStyle);
                return;
            }

            EnsureTableSetup();

            var tableRect = new Rect(x, y, w, yMax - y);
            EditorGUI.BeginChangeCheck();
            _tableView.Draw(tableRect, _cachedEntity.Components);
            if (EditorGUI.EndChangeCheck())
            {
                SyncComponentDataTypes();
                EditorUtility.SetDirty(_cachedEntity);
                if (_cachedPrefab != null) PrefabUtility.SavePrefabAsset(_cachedPrefab);
            }
        }

        // ── 表格初始化 ────────────────────────────────────────────────────────

        private void EnsureTableSetup()
        {
            if (_tableSetup) return;
            _tableSetup = true;

            _tableView.CanAdd         = true;
            _tableView.CanReorder     = true;
            _tableView.CanRemove      = true;
            _tableView.CanSelect      = false;
            _tableView.CanEdit        = true;
            _tableView.ShowToolbar    = false;
            _tableView.MarkDuplicates = false;
            _tableView.OnRowExpandField((rowIndex, _, anchorRect) => OpenConfigPopup(rowIndex, anchorRect));
        }

        // ── ComponentType 变更时重建 Data ──────────────────────────────────────

        private void SyncComponentDataTypes()
        {
            if (_cachedSo == null || _cachedEntity == null) return;

            _cachedSo.Update();
            var componentsProp = _cachedSo.FindProperty("Components");
            var anyFixed = false;

            for (var i = 0; i < _cachedEntity.Components.Count; i++)
            {
                var entry = _cachedEntity.Components[i];
                var expectedDataTypeName = entry.ComponentType + "Data";
                var dataMatchesType = string.IsNullOrEmpty(entry.ComponentType) ||
                                      entry.Data?.GetType().Name == expectedDataTypeName;

                if (dataMatchesType) continue;

                var dataProp = componentsProp
                    .GetArrayElementAtIndex(i)
                    .FindPropertyRelative("Data");

                var dataType = TypeCache.GetTypesDerivedFrom<BaseComponentData>()
                    .FirstOrDefault(t => t.Name == expectedDataTypeName);

                dataProp.managedReferenceValue = dataType != null
                    ? Activator.CreateInstance(dataType)
                    : null;

                anyFixed = true;
            }

            if (anyFixed) _cachedSo.ApplyModifiedProperties();
        }

        // ── 打开 Config 弹窗 ──────────────────────────────────────────────────

        private void OpenConfigPopup(int index, Rect anchorRect)
        {
            if (_cachedSo == null || _cachedEntity == null) return;
            if (index < 0 || index >= _cachedEntity.Components.Count) return;

            var entry = _cachedEntity.Components[index];
            if (entry.Data == null) return;

            _configPopup.OnClosed(() =>
            {
                entry.RefreshEntryKey();
                EditorUtility.SetDirty(_cachedEntity);
                if (_cachedPrefab != null) PrefabUtility.SavePrefabAsset(_cachedPrefab);
                DevWindow.Refresh();
            });
            _configPopup.Show(anchorRect, entry.Data);
        }

        // ── 挂载 Entity ──────────────────────────────────────────────────────

        private void AddEntity()
        {
            var contents = PrefabUtility.LoadPrefabContents(_cachedPath);
            contents.AddComponent<Entity>();
            PrefabUtility.SaveAsPrefabAsset(contents, _cachedPath);
            PrefabUtility.UnloadPrefabContents(contents);
            _cachedPath = null;
        }

        // ── Addressable actions ───────────────────────────────────────────────

        private void MarkAsAddressable()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("[PrefabViewer] 找不到 AddressableAssetSettings，请先在 Addressables 创建配置。");
                return;
            }

            const string groupName = "Prefab";
            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                var schemas = settings.DefaultGroup != null ? settings.DefaultGroup.Schemas : null;
                group = settings.CreateGroup(groupName, false, false, false, schemas);
            }

            var guid = AssetDatabase.AssetPathToGUID(_cachedPath);
            var entry = settings.CreateOrMoveEntry(guid, group);

            const string gamePrefix = "Assets/Game/";
            var address = _cachedPath.StartsWith(gamePrefix, StringComparison.OrdinalIgnoreCase)
                ? _cachedPath[gamePrefix.Length..]
                : _cachedPath;
            if (address.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                address = address[..^".prefab".Length];

            entry.address = address;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            RefreshAddressableInfo();
        }
    }
}
