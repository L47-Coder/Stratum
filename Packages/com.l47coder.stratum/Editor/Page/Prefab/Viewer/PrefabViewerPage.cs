using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class PrefabViewerPage : IPage
    {
        public string GroupTitle => "Prefab";
        public string TabTitle => "Viewer";

        private const float LeftPanelMin = 100f;
        private const float LeftPanelMax = 800f;
        private const float LeftPanelStart = 220f;

        private readonly PrefabViewerLeftPanel _leftPanel = new();
        private readonly PrefabViewerRightPanel _rightPanel = new();
        private SplitterHandle _splitter = new(LeftPanelStart);

        public void OnFirstEnter() => _leftPanel.OnFirstEnter(_rightPanel.SetPath);

        public void OnEnter() => _leftPanel.OnEnter();

        public void OnGUI(Rect rect)
        {
            var (leftRect, rightRect) = _splitter.Draw(rect, LeftPanelMin, LeftPanelMax);
            _leftPanel.OnGUI(leftRect);
            _rightPanel.OnGUI(rightRect);
        }
    }

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

    internal sealed class PrefabViewerRightPanel
    {
        private const float Padding = 8f;

        private static GUIStyle _addEntityTitleStyle;
        private static GUIStyle _addEntitySubStyle;

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
        private bool _pendingComponentDataSave;

        public void SetPath(string path)
        {
            if (_currentPath == path) return;
            _currentPath = path;
            _cachedPath = null;
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
                var inner = new Rect(rect.x + Padding, rect.y + Padding, rect.width - Padding * 2, rect.height - Padding * 2);
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

        private void DrawEntityConfigPanel(float x, float y, float w, float yMax)
        {
            if (_cachedEntity == null)
            {
                DrawAddEntityPlaceholder(x, y, w, yMax);
                return;
            }

            EnsureTableSetup();

            var tableRect = new Rect(x, y, w, yMax - y);
            _tableView.Draw(tableRect, _cachedEntity.Components);
            SavePendingComponentDataChanges();
        }

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
            _tableView.OnRowEdit(_ => MarkComponentDataChanged());
            _tableView.OnRowAdd(_ => MarkComponentDataChanged());
            _tableView.OnRowRemove(_ => MarkComponentDataChanged());
            _tableView.OnRowMove((_, _) => MarkComponentDataChanged());
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

        private void MarkComponentDataChanged() => _pendingComponentDataSave = true;

        private void SavePendingComponentDataChanges()
        {
            if (!_pendingComponentDataSave) return;
            _pendingComponentDataSave = false;

            SyncComponentDataTypes();
            EditorUtility.SetDirty(_cachedEntity);
            if (_cachedPrefab != null) PrefabUtility.SavePrefabAsset(_cachedPrefab);
        }

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
                    ? CreateComponentDataWithDefaultKey(dataType)
                    : null;

                anyFixed = true;
            }

            if (!anyFixed) return;

            _cachedSo.ApplyModifiedProperties();

            foreach (var entry in _cachedEntity.Components)
                entry.RefreshEntryKey();
        }

        private static object CreateComponentDataWithDefaultKey(Type dataType)
        {
            var data = Activator.CreateInstance(dataType);
            var keyField = dataType.GetField("Key", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (keyField?.FieldType == typeof(string))
                keyField.SetValue(data, "default");

            return data;
        }

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

            var iconContent = EditorGUIUtility.IconContent("Prefab Icon");
            var iconSize = 32f;
            var iconRect = new Rect(cursorX, y + (bannerH - iconSize) * 0.5f, iconSize, iconSize);
            GUI.DrawTexture(iconRect, iconContent.image as Texture2D, ScaleMode.ScaleToFit, true,
                            0f, new Color(1f, 1f, 1f, 0.45f), 0f, 0f);
            cursorX += iconSize + 12f;

            var textY = y + (bannerH - 34f) * 0.5f;
            GUI.Label(new Rect(cursorX, textY, w - 80f, 18f), "尚未挂载 Entity 组件", AddEntityTitleStyle);
            GUI.Label(new Rect(cursorX, textY + 18f, w - 80f, 16f), "点击此处添加 Entity 组件以在框架中管理其生命周期与组件配置。", AddEntitySubStyle);
        }

        private void AddEntity()
        {
            var contents = PrefabUtility.LoadPrefabContents(_cachedPath);
            contents.AddComponent<Entity>();
            PrefabUtility.SaveAsPrefabAsset(contents, _cachedPath);
            PrefabUtility.UnloadPrefabContents(contents);
            _cachedPath = null;
        }

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
