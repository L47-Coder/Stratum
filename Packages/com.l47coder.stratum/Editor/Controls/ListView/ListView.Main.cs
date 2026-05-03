#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class ListView
    {
        private const float RowHeight = 22f;
        private const string RenameControlName = "ListViewRenameField";
        private const string ReorderDragKey = "ListViewReorderToken";
        private const string ReorderFromKey = "ListViewReorderFromIndex";
        private const float ReorderDragThreshold = 4f;

        private static readonly int s_reorderControlHint = "ListViewReorderControl".GetHashCode();

        private string _renameLabel = "Rename";
        private string _deleteLabel = "Delete";
        private string _createLabel = "Create";
        private string _searchPlaceholder = "Search...";

        private int _selectedIndex = -1;
        private Vector2 _scrollPos;

        private Action<int> _onRowAdded;
        private Action<int> _onRowRemoved;
        private Action<int> _onRowSelected;
        private Action<int> _onRowRenamed;
        private Action<int, int> _onRowMoved;
        private Action<int> _onDropOnRow;
        private Action<int> _onButtonClicked;

        private readonly object _reorderToken = new();
        private int _reorderControlId;
        private int _pressDataIndex = -1;
        private Vector2 _pressPos;
        private int _reorderFromIndex = -1;
        private int _reorderInsertIndex = -1;

        private int _renamingIndex = -1;
        private string _renamingOriginal;
        private string _renameBuffer;
        private bool _renameFocusRequest;
        private bool _renameHadFocus;
        private int _renameGeneration;
        private string RenameCtrl => $"{RenameControlName}_{_renameGeneration}";

        private List<string> _renameBindingList;

        private bool _hasPendingContextMenu;
        private bool _pendingBodyContextMenu;
        private int _pendingContextIndex;
        private string _pendingContextLabel;
        private bool _pendingBeginRename;
        private bool _pendingDelete;

        private void BeginRename(List<string> items, int index, string label)
        {
            _renameBindingList = items;
            GUI.FocusControl(string.Empty);
            GUIUtility.keyboardControl = 0;
            UnityEditor.EditorGUIUtility.editingTextField = false;
            _renameGeneration++;
            _renamingIndex = index;
            _renamingOriginal = label;
            _renameBuffer = label;
            _renameFocusRequest = true;
            _renameHadFocus = false;
        }

        private void CommitRename(bool accept)
        {
            var idx = _renamingIndex;
            var orig = _renamingOriginal;
            var buf = _renameBuffer;
            var list = _renameBindingList;
            _renamingIndex = -1; _renamingOriginal = _renameBuffer = null;
            _renameFocusRequest = _renameHadFocus = false;
            _renameBindingList = null;
            if (!accept || idx < 0 || orig == null || buf == null || buf == orig || string.IsNullOrWhiteSpace(buf)) return;
            if (list != null && idx < list.Count) list[idx] = buf;
            _onRowRenamed?.Invoke(idx);
        }

        private void CheckRenameBlur()
        {
            if (_renamingIndex < 0 || !_renameHadFocus || Event.current.type == EventType.Layout) return;
            if (GUI.GetNameOfFocusedControl() != RenameCtrl) CommitRename(true);
        }

        private void TryRemoveRowAt(List<string> items, int index)
        {
            if (items == null || index < 0 || index >= items.Count) return;
            items.RemoveAt(index);
            if (_selectedIndex == index) _selectedIndex = items.Count > 0 ? Mathf.Min(index, items.Count - 1) : -1;
            else if (_selectedIndex > index) _selectedIndex--;
            _onRowRemoved?.Invoke(index);
            GUI.changed = true;
        }

        private void HandleKeyboard()
        {
            if (Event.current.type != EventType.KeyDown) return;
            if (_renamingIndex >= 0)
            {
                var kc = Event.current.keyCode;
                if (kc == KeyCode.Return || kc == KeyCode.KeypadEnter) { CommitRename(true); Event.current.Use(); }
                else if (kc == KeyCode.Escape) { CommitRename(false); Event.current.Use(); }
                return;
            }
            if (_selectedIndex < 0 || !CanSelect) return;
            if (Event.current.keyCode == KeyCode.F2 && CanRename) { _pendingBeginRename = true; Event.current.Use(); }
            if (Event.current.keyCode == KeyCode.Delete && CanRemove) { _pendingDelete = true; Event.current.Use(); }
        }
    }
}
#endif
