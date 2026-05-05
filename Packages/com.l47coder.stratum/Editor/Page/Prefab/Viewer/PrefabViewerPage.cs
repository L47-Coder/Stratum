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
        private const float Padding = 8f;
        private const float RowH = 20f;
        private const float TitleRowH = 18f;
        private const float PreviewSize = 64f;

        private const double SaveDelay = 0.5;

        private static GUIStyle _keyLabelStyle;
        private static GUIStyle _hintLabelStyle;
        private static GUIStyle _addrHintStyle;

        private static GUIStyle _titleStyle;

        private static GUIStyle TitleStyle =>
            _titleStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                clipping = TextClipping.Clip,
            };

        private static GUIStyle KeyLabelStyle =>
            _keyLabelStyle ??= new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.58f, 0.58f, 0.58f) } };

        private static GUIStyle HintLabelStyle =>
            _hintLabelStyle ??= new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.65f, 0.65f, 0.65f) } };

        private static GUIStyle AddrHintStyle =>
            _addrHintStyle ??= new GUIStyle(EditorStyles.label)
            { normal = { textColor = new Color(0.68f, 0.68f, 0.68f) } };

        private static readonly Color HeaderBg = new(0.105f, 0.105f, 0.11f);
        private static readonly Color HeaderRule = new(0.2f, 0.2f, 0.22f);
        private static readonly Color ThumbBorder = new(0.06f, 0.06f, 0.07f);
        private static readonly Color ThumbBackdrop = new(0.15f, 0.15f, 0.16f);
        private string _currentPath;
        private string _cachedPath;
        private GameObject _cachedPrefab;
        private Texture2D _cachedPreview;
        private Entity _cachedEntity;
        private SerializedObject _cachedSo;

        private bool _isAddressable;
        private string _addrAddress;
        private string _addrGroup;
        private string _addrLabels;

        private readonly TableControl _tableView   = new();
        private readonly FieldPopup   _configPopup = new();
        private bool _tableSetup;

        private bool _pendingSave;
        private double _saveScheduledAt;

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
            _cachedPreview = null;
            _cachedEntity = _cachedPrefab != null ? _cachedPrefab.GetComponent<Entity>() : null;
            _cachedSo = _cachedEntity != null ? new SerializedObject(_cachedEntity) : null;
            _pendingSave = false;

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
            if (_pendingSave && Event.current.type == EventType.Repaint &&
                EditorApplication.timeSinceStartup >= _saveScheduledAt)
            {
                _pendingSave = false;
                AssetDatabase.SaveAssets();
            }

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
            var infoRows = !_isAddressable ? 1 : (string.IsNullOrEmpty(_addrLabels) ? 2 : 3);
            var rightH = Padding + TitleRowH + 6f + infoRows * RowH + 8f + 22f + Padding;
            var leftH = Padding + PreviewSize + Padding;
            return Mathf.Max(leftH, rightH);
        }

        // ── Addressable header ────────────────────────────────────────────────

        private void DrawAddressableHeader(float x, float y, float w, float h)
        {
            EditorGUI.DrawRect(new Rect(x, y, w, h), HeaderBg);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(x, y + h - 1f, w, 1f), HeaderRule);

            if (_cachedPreview == null)
                _cachedPreview = AssetPreview.GetAssetPreview(_cachedPrefab);

            var thumbOuter = new Rect(x + Padding, y + Padding, PreviewSize, PreviewSize);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(thumbOuter.x - 1f, thumbOuter.y - 1f, thumbOuter.width + 2f, thumbOuter.height + 2f), ThumbBorder);
                EditorGUI.DrawRect(thumbOuter, ThumbBackdrop);
            }

            var thumbInner = new Rect(thumbOuter.x + 1f, thumbOuter.y + 1f, thumbOuter.width - 2f, thumbOuter.height - 2f);
            if (_cachedPreview != null)
                GUI.DrawTexture(thumbInner, _cachedPreview, ScaleMode.ScaleToFit);
            else if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(thumbInner, new Color(0.2f, 0.2f, 0.21f));

            var rightX = thumbOuter.xMax + Padding + 2f;
            var rightW = Mathf.Max(40f, x + w - rightX - Padding);
            var ry = y + Padding;

            var prefabTitle = Path.GetFileNameWithoutExtension(_cachedPath) ?? "Prefab";
            GUI.Label(new Rect(rightX, ry, rightW, TitleRowH), prefabTitle, TitleStyle);
            ry += TitleRowH + 6f;

            if (!_isAddressable)
            {
                EditorGUI.LabelField(new Rect(rightX, ry, rightW, RowH),
                    "尚未加入 Addressable 组", AddrHintStyle);
                ry += RowH + 8f;
                if (GUI.Button(new Rect(rightX, ry, rightW, 22f), "标记为 Addressable", EditorStyles.miniButton))
                    MarkAsAddressable();
            }
            else
            {
                DrawKeyValue(ref ry, rightX, rightW, "Address", _addrAddress);
                DrawKeyValue(ref ry, rightX, rightW, "Group", _addrGroup);
                if (!string.IsNullOrEmpty(_addrLabels))
                    DrawKeyValue(ref ry, rightX, rightW, "Labels", _addrLabels);
                ry += 8f;
                if (GUI.Button(new Rect(rightX, ry, rightW, 22f), "在 Dev Workbench 中查看", EditorStyles.miniButton))
                    AddressableViewerPage.NavigateFromPrefab(_addrGroup);
            }
        }

        private static void DrawKeyValue(ref float y, float x, float w, string key, string value)
        {
            const float keyW = 64f;
            EditorGUI.LabelField(new Rect(x, y, keyW, RowH), key, KeyLabelStyle);
            EditorGUI.LabelField(new Rect(x + keyW + 4f, y, w - keyW - 4f, RowH),
                value, EditorStyles.miniLabel);
            y += RowH;
        }

        // ── Entity / EntityComponentEntry 表格 ────────────────────────────────

        private void DrawEntityConfigPanel(float x, float y, float w, float yMax)
        {
            if (_cachedEntity == null)
            {
                var pad = Padding;
                EditorGUI.LabelField(new Rect(x + pad, y + pad, w - pad * 2, RowH),
                    "此预制体尚未挂载 Entity 组件", HintLabelStyle);
                y += pad + RowH + pad;
                if (GUI.Button(new Rect(x + pad, y, 200f, 22f), "挂载 Entity 组件", EditorStyles.miniButton))
                    AddEntity();
                return;
            }

            EnsureTableSetup();

            var tableRect = new Rect(x, y, w, yMax - y);
            _tableView.Draw(tableRect, _cachedEntity.Components);

            if (GUI.changed)
            {
                SyncComponentDataTypes();
                EditorUtility.SetDirty(_cachedEntity);
                _pendingSave = true;
                _saveScheduledAt = EditorApplication.timeSinceStartup + SaveDelay;
            }
        }

        // ── 表格初始化 ────────────────────────────────────────────────────────

        private void EnsureTableSetup()
        {
            if (_tableSetup) return;
            _tableSetup = true;

            _tableView.CanAdd         = true;
            _tableView.CanDrag        = true;
            _tableView.CanRemove      = true;
            _tableView.CanSelect      = false;
            _tableView.CanRename      = true;
            _tableView.ShowToolbar    = false;
            _tableView.MarkDuplicates = false;
            _tableView.OnExpandFieldAt((rowIndex, _, anchorRect) => OpenConfigPopup(rowIndex, anchorRect));
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

            var data = _cachedEntity.Components[index].Data;
            if (data == null) return;

            var so        = _cachedSo;
            var listProp  = so.FindProperty("Components");
            var dataProp  = listProp.GetArrayElementAtIndex(index).FindPropertyRelative("Data");

            _configPopup.OnChanged(() =>
            {
                so.Update();
                dataProp.managedReferenceValue = data;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(_cachedEntity);
                _pendingSave     = true;
                _saveScheduledAt = EditorApplication.timeSinceStartup + SaveDelay;
            });
            _configPopup.Show(anchorRect, data);
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
            var guid = AssetDatabase.AssetPathToGUID(_cachedPath);
            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = Path.GetFileNameWithoutExtension(_cachedPath);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            RefreshAddressableInfo();
        }
    }
}
