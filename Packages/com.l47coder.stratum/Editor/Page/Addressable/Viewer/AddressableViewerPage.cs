using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class AddressableViewerPage : IPage
    {
        public string GroupTitle => "Addressable";
        public string TabTitle => "Viewer";

        internal static string PendingSelectGroupName;
        internal static string PendingSelectAssetPath;

        internal static void NavigateFromPrefab(string addressableGroupName, string assetPath)
        {
            PendingSelectGroupName = string.IsNullOrEmpty(addressableGroupName) ? null : addressableGroupName;
            PendingSelectAssetPath = string.IsNullOrEmpty(assetPath) ? null : assetPath;
            DevWindow.GoTo("Addressable", "Viewer");
        }

        internal static void FinishPendingGroupSelection(AddressableViewerPage page)
        {
            if (page == null || string.IsNullOrEmpty(PendingSelectGroupName)) return;
            var name = PendingSelectGroupName;
            var assetPath = PendingSelectAssetPath;
            PendingSelectGroupName = null;
            PendingSelectAssetPath = null;
            page._leftPanel.TrySelectGroupByName(name);
            if (!string.IsNullOrEmpty(assetPath))
                page._rightPanel.TrySelectByAssetPath(assetPath);
        }

        public void OnEnter() => FinishPendingGroupSelection(this);

        private const float SplitterVisualW = 1f;
        private const float LeftPanelMin = 100f;
        private const float LeftPanelMax = 800f;
        private const float LeftPanelStart = 180f;
        private static readonly Color SplitterColor = new(0.11f, 0.11f, 0.11f);

        private readonly AddressableGroupPanel _leftPanel = new();
        private readonly AddressableEntryPanel _rightPanel = new();
        private float _splitterX = LeftPanelStart;
        private bool _dragging;

        public void OnFirstEnter() => _leftPanel.OnFirstEnter(_rightPanel.SetGroup, _rightPanel.Invalidate);

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
            var rightRect = new Rect(visualRect.xMax, rect.y, rect.width - _splitterX - SplitterVisualW, rect.height);

            _leftPanel.OnGUI(leftRect);
            EditorGUI.DrawRect(visualRect, SplitterColor);
            _rightPanel.OnGUI(rightRect);
        }
    }

    internal sealed class AddressableGroupPanel
    {
        private readonly ListControl _listView = new()
        {
            CanReceiveDrop = true,
            CanReorder = true,
        };

        private List<AddressableAssetGroup> _visibleGroups = new();
        private readonly List<string> _groupNames = new();
        private int _knownGroupCount = -1;

        private Action<AddressableAssetGroup> _onGroupSelected;
        private Action _onDropComplete;

        public void OnFirstEnter(Action<AddressableAssetGroup> onGroupSelected, Action onDropComplete)
        {
            _onGroupSelected = onGroupSelected;
            _onDropComplete = onDropComplete;

            _listView.OnRowSelect(idx =>
                _onGroupSelected?.Invoke(idx >= 0 && idx < _visibleGroups.Count ? _visibleGroups[idx] : null));

            _listView.OnRowReceiveDrop(HandleDropOnGroup);
            _listView.OnRowEdit(HandleRenameGroup);
            _listView.OnRowRemove(HandleDeleteGroup);
            _listView.OnRowMove(HandleReorderGroup);
            _listView.OnRowAdd(HandleAddGroup);
        }

        public void OnGUI(Rect rect)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                var inner = new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f);
                GUI.Label(inner,
                    "AddressableAssetSettings not found.\nUse Window → Asset Management → Addressables → Groups to create the configuration first.",
                    EditorStyles.wordWrappedLabel);
                return;
            }

            if (settings.groups.Count != _knownGroupCount)
            {
                _knownGroupCount = settings.groups.Count;
                RebuildGroupList(settings);
            }

            _listView.Draw(rect, _groupNames);
        }

        internal void TrySelectGroupByName(string groupName)
        {
            if (string.IsNullOrEmpty(groupName)) return;
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            _knownGroupCount = settings.groups.Count;
            RebuildGroupList(settings);

            for (var i = 0; i < _visibleGroups.Count; i++)
            {
                if (string.Equals(_visibleGroups[i].Name, groupName, StringComparison.Ordinal))
                {
                    _listView.SelectRow(i);
                    return;
                }
            }
        }

        private void RebuildGroupList(AddressableAssetSettings settings)
        {
            _visibleGroups = GetSortedVisibleGroups(settings);
            _groupNames.Clear();
            foreach (var g in _visibleGroups)
                _groupNames.Add(g.Name);
        }

        private static List<AddressableAssetGroup> GetSortedVisibleGroups(AddressableAssetSettings settings)
        {
            var config = AssetDatabase.LoadAssetAtPath<AddressableGroupOrderConfig>(WorkbenchPaths.AddressableGroupOrder);
            var order = config?.GroupGuids ?? new List<string>();

            var orderIdx = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < order.Count; i++)
                if (!string.IsNullOrEmpty(order[i]))
                    orderIdx[order[i]] = i;

            return settings.groups
                .Where(g => g != null
                    && !string.Equals(g.Name, "Built In Data", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(g.Name, "Frame", StringComparison.OrdinalIgnoreCase))
                .OrderBy(g => orderIdx.TryGetValue(g.Guid, out var i) ? i : int.MaxValue)
                .ThenBy(g => g.Name, StringComparer.Ordinal)
                .ToList();
        }

        private static AddressableGroupOrderConfig LoadGroupOrderConfig() =>
            AssetDatabase.LoadAssetAtPath<AddressableGroupOrderConfig>(WorkbenchPaths.AddressableGroupOrder);

        private void HandleAddGroup(int idx)
        {
            var s = AddressableAssetSettingsDefaultObject.Settings;
            if (s == null) { _groupNames.RemoveAt(idx); return; }

            var name = "New Group";
            var counter = 0;
            while (s.groups.Any(g => g.Name == name))
                name = $"New Group {++counter}";

            s.CreateGroup(name, false, false, false, null);
            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssets();

            _visibleGroups = GetSortedVisibleGroups(s);
            _groupNames.Clear();
            foreach (var g in _visibleGroups) _groupNames.Add(g.Name);
            _knownGroupCount = s.groups.Count;
        }

        private void HandleRenameGroup(int idx)
        {
            if (idx < 0 || idx >= _visibleGroups.Count) return;
            var group = _visibleGroups[idx];
            var newName = _groupNames[idx];
            if (string.IsNullOrWhiteSpace(newName) || group.Name == newName) return;

            try
            {
                AssetDatabase.StartAssetEditing();
                group.Name = newName;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            EditorUtility.SetDirty(group);
            AssetDatabase.SaveAssets();
        }

        private void HandleDeleteGroup(int idx)
        {
            if (idx < 0 || idx >= _visibleGroups.Count) return;
            var group = _visibleGroups[idx];

            if (!EditorUtility.DisplayDialog("Confirm deletion",
                $"Delete group \"{group.Name}\"? Its entries will be removed, but the underlying asset files will not be touched.",
                "Delete", "Cancel"))
            {
                _groupNames.Insert(idx, group.Name);
                return;
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { _groupNames.Insert(idx, group.Name); return; }

            settings.RemoveGroup(group);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            _visibleGroups.RemoveAt(idx);
            _knownGroupCount = settings.groups.Count;

            var config = LoadGroupOrderConfig();
            config.GroupGuids.Remove(group.Guid);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        private void HandleReorderGroup(int from, int to)
        {
            if (from < 0 || from >= _visibleGroups.Count) return;

            var group = _visibleGroups[from];
            _visibleGroups.RemoveAt(from);
            _visibleGroups.Insert(Mathf.Clamp(to, 0, _visibleGroups.Count), group);

            var config = LoadGroupOrderConfig();
            config.GroupGuids.Clear();
            foreach (var g in _visibleGroups) config.GroupGuids.Add(g.Guid);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        private void HandleDropOnGroup(int targetIdx)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;
            if (targetIdx < 0 || targetIdx >= _visibleGroups.Count) return;

            var guid = DragAndDrop.GetGenericData("AddressableEntryGuid") as string;
            if (string.IsNullOrEmpty(guid)) return;

            var entry = settings.FindAssetEntry(guid);
            var targetGroup = _visibleGroups[targetIdx];
            if (entry == null || entry.parentGroup == targetGroup) return;

            settings.MoveEntry(entry, targetGroup);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            _onDropComplete?.Invoke();
        }
    }

    internal sealed class AddressableEntryRow
    {
        [Field(Title = "Address", Width = 300)]
        public string Address;

        [Field(Title = "AssetPath", Readonly = true, Width = 450)]
        public string AssetPath;

        [Field(Title = "Labels", Width = 200)]
        [Dropdown(nameof(GetAllLabels), Multi = true)]
        public string Labels;

        [Field(Hide = true)]
        public string Guid;

        private static string[] GetAllLabels() =>
            AddressableAssetSettingsDefaultObject.Settings?.GetLabels()?.ToArray()
            ?? Array.Empty<string>();
    }

    internal sealed class AddressableEntryPanel
    {
        private readonly TableControl _tableView = new()
        {
            CanAdd = false,
            CanRemove = false,
            CanReorder = false,
            CanDragOut = true,
            KeyField = "Address",
        };

        private AddressableAssetGroup _currentGroup;
        private AddressableAssetGroup _cachedGroup;
        private int _cachedEntryCount = -1;
        private readonly List<AddressableEntryRow> _rows = new();
        private readonly List<AddressableAssetEntry> _entries = new();

        public AddressableEntryPanel()
        {
            // 行拖出控件时：设置跨控件拖拽载荷
            _tableView.OnRowDragOut(idx =>
            {
                if (idx >= 0 && idx < _rows.Count)
                    DragAndDrop.SetGenericData("AddressableEntryGuid", _rows[idx].Guid);
            });

            // 行在组内重排时：同步 _entries，保证 SyncAllEntries 的索引对应关系正确
            _tableView.OnRowMove((from, to) =>
            {
                if (from < 0 || from >= _entries.Count) return;
                var entry = _entries[from];
                _entries.RemoveAt(from);
                _entries.Insert(Mathf.Clamp(to, 0, _entries.Count), entry);
            });
        }

        public void SetGroup(AddressableAssetGroup group) => _currentGroup = group;

        public void Invalidate() => _cachedEntryCount = -1;

        public void TrySelectByAssetPath(string assetPath)
        {
            if (_currentGroup != null && (_cachedGroup != _currentGroup || _cachedEntryCount != _currentGroup.entries.Count))
            {
                _cachedGroup = _currentGroup;
                _cachedEntryCount = _currentGroup.entries.Count;
                RebuildRows(_currentGroup);
            }

            for (var i = 0; i < _rows.Count; i++)
            {
                if (string.Equals(_rows[i].AssetPath, assetPath, StringComparison.OrdinalIgnoreCase))
                {
                    _tableView.SelectRow(i);
                    return;
                }
            }
        }

        public void OnGUI(Rect rect)
        {
            var group = _currentGroup;
            if (group == null)
            {
                GUI.Label(rect, "No group selected", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            if (_cachedGroup != group || _cachedEntryCount != group.entries.Count)
            {
                _cachedGroup = group;
                _cachedEntryCount = group.entries.Count;
                RebuildRows(group);
            }

            _tableView.Draw(rect, _rows);
            SyncAllEntries();
        }

        private void RebuildRows(AddressableAssetGroup group)
        {
            _rows.Clear();
            _entries.Clear();
            foreach (var entry in group.entries)
            {
                _entries.Add(entry);
                _rows.Add(new AddressableEntryRow
                {
                    Address = entry.address,
                    AssetPath = entry.AssetPath,
                    Labels = string.Join(", ", entry.labels),
                    Guid = entry.guid,
                });
            }
        }

        private void SyncAllEntries()
        {
            var anyDirty = false;
            for (var i = 0; i < _rows.Count; i++)
                if (SyncEntryFromRow(i, _rows[i])) anyDirty = true;
            if (anyDirty) AssetDatabase.SaveAssets();
        }

        private bool SyncEntryFromRow(int index, AddressableEntryRow row)
        {
            if (index < 0 || index >= _entries.Count) return false;

            var entry = _entries[index];
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return false;

            var dirty = false;

            if (row.Address != entry.address)
            {
                entry.address = row.Address;
                dirty = true;
            }

            var newLabels = (row.Labels ?? string.Empty)
                .Split(',')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var l in entry.labels.Where(l => !newLabels.Contains(l)).ToList())
            {
                entry.SetLabel(l, false);
                dirty = true;
            }
            foreach (var l in newLabels.Where(l => !entry.labels.Contains(l)))
            {
                settings.AddLabel(l);
                entry.SetLabel(l, true);
                dirty = true;
            }

            if (dirty) EditorUtility.SetDirty(entry.parentGroup);
            return dirty;
        }
    }
}
