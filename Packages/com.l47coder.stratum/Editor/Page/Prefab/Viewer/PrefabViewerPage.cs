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

        private static GUIStyle _hintLabelCenterStyle;
        private static GUIStyle _addEntityTitleStyle;
        private static GUIStyle _addEntitySubStyle;

        private static GUIStyle HintLabelCenterStyle =>
            _hintLabelCenterStyle ??= new GUIStyle(EditorStyles.miniLabel)
            { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.65f, 0.65f, 0.65f) } };

        private static GUIStyle AddEntityTitleStyle => _addEntityTitleStyle ??= new GUIStyle(EditorStyles.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.85f, 0.85f, 0.85f) },
        };

        private static GUIStyle AddEntitySubStyle => _addEntitySubStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.55f, 0.55f, 0.55f) },
        };

        private static readonly Color AddrLinkRowBg = new(0.115f, 0.115f, 0.12f);
        private static readonly Color AddrLinkRowBgHover = new(0.145f, 0.15f, 0.168f);
        private static readonly Color CardBg = new(0.13f, 0.13f, 0.14f);
        private static readonly Color CardBorder = new(0.22f, 0.22f, 0.24f);

        private string _currentPath;
        private string _cachedPath;
        private GameObject _cachedPrefab;
        private Entity _cachedEntity;
        private SerializedObject _cachedSo;

        private bool _isAddressable;
        private string _addrGroup;

        private readonly TableControl _tableView = new();
        private readonly FieldPopup _configPopup = new();
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

            DrawEntityConfigPanel(rect.x, rect.y, rect.width, rect.yMax);
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
            _addrGroup = string.Empty;

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            var guid = AssetDatabase.AssetPathToGUID(_currentPath);
            var entry = settings.FindAssetEntry(guid);
            if (entry == null) return;

            _isAddressable = true;
            _addrGroup = entry.parentGroup?.Name ?? string.Empty;
        }

        // ── Entity / EntityComponentEntry 表格 ────────────────────────────────

        private void DrawEntityConfigPanel(float x, float y, float w, float yMax)
        {
            if (_cachedEntity == null)
            {
                DrawAddEntityPlaceholder(x, y, w, yMax);
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

            _tableView.CanAdd = true;
            _tableView.CanReorder = true;
            _tableView.CanRemove = true;
            _tableView.CanSelect = false;
            _tableView.CanEdit = true;
            _tableView.ShowToolbar = true;
            _tableView.MarkDuplicates = true;
            _tableView.KeyField = "EntryKey";
            _tableView.ToolbarButtons.Add(new GUIContent(
                EditorGUIUtility.IconContent("d_Linked").image,
                "在 Addressable Viewer 中查看"));
            _tableView.OnRowExpandField((rowIndex, _, anchorRect) => OpenConfigPopup(rowIndex, anchorRect));
            _tableView.OnButtonClick(idx =>
            {
                if (idx != 0) return;
                if (_isAddressable)
                    AddressableViewerPage.NavigateFromPrefab(_addrGroup, _cachedPath);
                else
                    MarkAsAddressable();
            });
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

            if (!anyFixed) return;

            _cachedSo.ApplyModifiedProperties();

            foreach (var entry in _cachedEntity.Components)
                entry.RefreshEntryKey();
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

        // ── 无 Entity 占位视图 ────────────────────────────────────────────────

        private void DrawAddEntityPlaceholder(float x, float y, float w, float yMax)
        {
            const float bannerH = 56f;
            var bannerRect = new Rect(x, y, w, bannerH);
            var hover = bannerRect.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(bannerRect, hover ? new Color(0.16f, 0.16f, 0.17f) : CardBg);
                EditorGUI.DrawRect(new Rect(x, y + bannerH - 1, w, 1), CardBorder);
            }

            EditorGUIUtility.AddCursorRect(bannerRect, MouseCursor.Link);

            if (Event.current.type == EventType.MouseDown && hover && Event.current.button == 0)
            {
                AddEntity();
                Event.current.Use();
            }

            var cursorX = x + 16f;

            // 图标
            var iconContent = EditorGUIUtility.IconContent("Prefab Icon");
            var iconSize = 32f;
            var iconRect = new Rect(cursorX, y + (bannerH - iconSize) * 0.5f, iconSize, iconSize);
            GUI.DrawTexture(iconRect, iconContent.image as Texture2D, ScaleMode.ScaleToFit, true,
                            0f, new Color(1f, 1f, 1f, 0.45f), 0f, 0f);
            cursorX += iconSize + 12f;

            // 文字区域
            var textY = y + (bannerH - 34f) * 0.5f;
            GUI.Label(new Rect(cursorX, textY, w - 80f, 18f), "尚未挂载 Entity 组件", AddEntityTitleStyle);
            GUI.Label(new Rect(cursorX, textY + 18f, w - 80f, 16f), "点击此处添加 Entity 组件以在框架中管理其生命周期与组件配置。", AddEntitySubStyle);
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
