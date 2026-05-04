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
        public string TabTitle   => "Viewer";

        private const float SplitterVisualW = 1f;
        private const float LeftPanelMin    = 100f;
        private const float LeftPanelMax    = 800f;
        private const float LeftPanelStart  = 220f;
        private static readonly Color SplitterColor = new(0.11f, 0.11f, 0.11f);

        private readonly PrefabViewerLeftPanel  _leftPanel  = new();
        private readonly PrefabViewerRightPanel _rightPanel = new();
        private float _splitterX = LeftPanelStart;
        private bool  _dragging;

        public void OnFirstEnter() => _leftPanel.OnFirstEnter(_rightPanel.SetPath);

        public void OnGUI(Rect rect)
        {
            var visualRect = new Rect(rect.x + _splitterX, rect.y, SplitterVisualW, rect.height);
            var hitRect    = new Rect(rect.x + _splitterX - 2f, rect.y, SplitterVisualW + 4f, rect.height);

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

            var leftRect  = new Rect(rect.x, rect.y, _splitterX, rect.height);
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
        private readonly TreeView _treeView = new();

        public void OnFirstEnter(Action<string> onSelected)
        {
            _treeView.HiddenExtensions = new() { ".prefab" };
            _treeView.CanAdd = true;
            _treeView.OnNodeSelected(onSelected);
        }

        public void OnGUI(Rect rect) =>
            _treeView.Draw(rect, WorkbenchPaths.PrefabRoot);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Right Panel
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class PrefabViewerRightPanel
    {
        private const float Padding       = 8f;
        private const float RowH          = 22f;
        private const float SectionLabelH = 22f;
        private const float PreviewSize   = 56f;

        private const float TableBoxOverhead = (BoxDrawer.Padding + BoxDrawer.BorderWidth) * 2f;
        private const float TableRowH        = 26f;
        private const double SaveDelay       = 0.5;

        private static GUIStyle _keyLabelStyle;
        private static GUIStyle _hintLabelStyle;
        private static GUIStyle _addrHintStyle;

        private static GUIStyle KeyLabelStyle =>
            _keyLabelStyle ??= new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = new Color(0.58f, 0.58f, 0.58f) } };

        private static GUIStyle HintLabelStyle =>
            _hintLabelStyle ??= new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = new Color(0.65f, 0.65f, 0.65f) } };

        private static GUIStyle AddrHintStyle =>
            _addrHintStyle ??= new GUIStyle(EditorStyles.label)
                { normal = { textColor = new Color(0.68f, 0.68f, 0.68f) } };

        private static readonly Color HeaderBg  = new(0.13f, 0.13f, 0.13f);
        private static readonly Color SectionBg = new(0.14f, 0.14f, 0.15f);

        private string           _currentPath;
        private string           _cachedPath;
        private GameObject       _cachedPrefab;
        private Texture2D        _cachedPreview;
        private Entity           _cachedEntity;
        private SerializedObject _cachedSo;
        private Vector2          _scrollPos;

        private bool   _isAddressable;
        private string _addrAddress;
        private string _addrGroup;
        private string _addrLabels;

        private readonly TableView _tableView = new();
        private bool               _tableSetup;

        private bool   _pendingSave;
        private double _saveScheduledAt;

        // ── Public API ───────────────────────────────────────────────────────

        public void SetPath(string path)
        {
            if (_currentPath == path) return;
            _currentPath = path;
            _cachedPath  = null;
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

            _cachedPath    = _currentPath;
            _cachedPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>(_currentPath);
            _cachedPreview = null;
            _cachedEntity  = _cachedPrefab != null ? _cachedPrefab.GetComponent<Entity>() : null;
            _cachedSo      = _cachedEntity != null ? new SerializedObject(_cachedEntity) : null;
            _pendingSave   = false;

            RefreshAddressableInfo();
        }

        private void RefreshAddressableInfo()
        {
            _isAddressable = false;
            _addrAddress   = string.Empty;
            _addrGroup     = string.Empty;
            _addrLabels    = string.Empty;

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            var guid  = AssetDatabase.AssetPathToGUID(_currentPath);
            var entry = settings.FindAssetEntry(guid);
            if (entry == null) return;

            _isAddressable = true;
            _addrAddress   = entry.address;
            _addrGroup     = entry.parentGroup?.Name ?? string.Empty;
            _addrLabels    = string.Join(", ", entry.labels);
        }

        // ── Panel layout ──────────────────────────────────────────────────────

        private void DrawPanel(Rect rect)
        {
            if (_pendingSave && Event.current.type == EventType.Repaint &&
                EditorApplication.timeSinceStartup >= _saveScheduledAt)
            {
                _pendingSave = false;
                AssetDatabase.SaveAssets();
            }

            var addrRowCount = _isAddressable
                ? 2 + (string.IsNullOrEmpty(_addrLabels) ? 0 : 1)
                : 1;
            var addrHeaderH = Padding + addrRowCount * RowH + Padding + 22f + Padding;

            var configH     = CalcConfigSectionHeight();
            var contentH    = addrHeaderH + Padding + configH + Padding;
            var contentRect = new Rect(0f, 0f, rect.width - 16f, Mathf.Max(contentH, rect.height));
            _scrollPos = GUI.BeginScrollView(rect, _scrollPos, contentRect);

            var y = 0f;
            var w = contentRect.width;

            DrawAddressableHeader(ref y, w, addrHeaderH);
            y += Padding;
            DrawEntityConfigPanel(ref y, w);

            GUI.EndScrollView();
        }

        private float CalcConfigSectionHeight()
        {
            if (_cachedEntity == null)
                return SectionLabelH + RowH + Padding + 22f + Padding;

            var rowCount = Mathf.Max(1, _cachedEntity.Components.Count);
            return SectionLabelH + TableBoxOverhead + ControlsToolbar.ToolbarHeight + TableRowH * (1 + rowCount) + Padding;
        }

        // ── Addressable header ────────────────────────────────────────────────

        private void DrawAddressableHeader(ref float y, float w, float h)
        {
            EditorGUI.DrawRect(new Rect(0f, y, w, h), HeaderBg);

            if (_cachedPreview == null)
                _cachedPreview = AssetPreview.GetAssetPreview(_cachedPrefab);

            var thumbY    = y + (h - PreviewSize) * 0.5f;
            var thumbRect = new Rect(Padding, thumbY, PreviewSize, PreviewSize);
            if (_cachedPreview != null)
                GUI.DrawTexture(thumbRect, _cachedPreview, ScaleMode.ScaleToFit);
            else
                EditorGUI.DrawRect(thumbRect, new Color(0.22f, 0.22f, 0.22f));

            var rightX = Padding + PreviewSize + Padding;
            var rightW = w - rightX - Padding;
            var ry     = y + Padding;

            if (!_isAddressable)
            {
                EditorGUI.LabelField(new Rect(rightX, ry, rightW, RowH),
                    "此预制体尚未标记为 Addressable", AddrHintStyle);
                ry += RowH + Padding;
                if (GUI.Button(new Rect(rightX, ry, 140f, 22f), "标记为 Addressable", EditorStyles.miniButton))
                    MarkAsAddressable();
            }
            else
            {
                DrawKeyValue(ref ry, rightX, rightW, "Address", _addrAddress);
                DrawKeyValue(ref ry, rightX, rightW, "Group",   _addrGroup);
                if (!string.IsNullOrEmpty(_addrLabels))
                    DrawKeyValue(ref ry, rightX, rightW, "Labels", _addrLabels);
                ry += Padding;
                if (GUI.Button(new Rect(rightX, ry, 140f, 22f), "在 Addressables 中查看", EditorStyles.miniButton))
                    EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
            }

            y += h;
        }

        private static void DrawKeyValue(ref float y, float x, float w, string key, string value)
        {
            const float keyW = 52f;
            EditorGUI.LabelField(new Rect(x, y, keyW, RowH), key, KeyLabelStyle);
            EditorGUI.LabelField(new Rect(x + keyW + 4f, y, w - keyW - 4f, RowH),
                value, EditorStyles.miniLabel);
            y += RowH;
        }

        // ── Entity / EntityComponentEntry 表格 ────────────────────────────────

        private void DrawEntityConfigPanel(ref float y, float w)
        {
            EditorGUI.DrawRect(new Rect(0f, y, w, SectionLabelH), SectionBg);
            EditorGUI.LabelField(new Rect(Padding, y + 2f, w - Padding * 2, SectionLabelH - 2f),
                "组件配置", EditorStyles.boldLabel);
            y += SectionLabelH;

            if (_cachedEntity == null)
            {
                EditorGUI.LabelField(new Rect(Padding, y + 2f, w - Padding * 2, RowH - 4f),
                    "此预制体尚未挂载 Entity 组件", HintLabelStyle);
                y += RowH + Padding;
                if (GUI.Button(new Rect(Padding, y, 140f, 22f), "挂载 Entity 组件", EditorStyles.miniButton))
                    AddEntity();
                y += 22f + Padding;
                return;
            }

            EnsureTableSetup();

            var rowCount  = Mathf.Max(1, _cachedEntity.Components.Count);
            var tableH    = TableBoxOverhead + ControlsToolbar.ToolbarHeight + TableRowH * (1 + rowCount);
            var tableRect = new Rect(0f, y, w, tableH);

            _tableView.Draw(tableRect, _cachedEntity.Components);

            if (GUI.changed)
            {
                SyncComponentDataTypes();
                EditorUtility.SetDirty(_cachedEntity);
                _pendingSave     = true;
                _saveScheduledAt = EditorApplication.timeSinceStartup + SaveDelay;
            }

            y += tableH + Padding;
        }

        // ── 表格初始化 ────────────────────────────────────────────────────────

        private void EnsureTableSetup()
        {
            if (_tableSetup) return;
            _tableSetup = true;

            _tableView.CanAdd    = true;
            _tableView.CanDrag   = true;
            _tableView.CanRemove = true;
            _tableView.CanSelect = false;
            _tableView.CanRename = false;
            _tableView.KeyField  = "ComponentType";
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

        // ── 打开 Config 弹窗（供后续需求使用）─────────────────────────────────

        private void OpenConfigPopup(int index)
        {
            if (_cachedSo == null || _cachedEntity == null) return;
            if (index < 0 || index >= _cachedEntity.Components.Count) return;
            ComponentConfigPopup.Open(_cachedSo, index);
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
            var guid  = AssetDatabase.AssetPathToGUID(_cachedPath);
            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = Path.GetFileNameWithoutExtension(_cachedPath);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            RefreshAddressableInfo();
        }
    }
}
