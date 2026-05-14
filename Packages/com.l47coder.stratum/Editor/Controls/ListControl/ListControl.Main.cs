using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class ListControl
    {
        private const float RowHeight = 22f;
        private const string RenameControlName = "ListViewRenameField";
        private const string ReorderDragKey = "ListViewReorderToken";
        private const string ReorderFromKey = "ListViewReorderFromIndex";
        private const float ReorderDragThreshold = 4f;

        private static readonly int s_reorderControlHint = "ListViewReorderControl".GetHashCode();

        private readonly string _renameLabel = "Rename";
        private readonly string _deleteLabel = "Delete";
        private readonly string _createLabel = "Create";
        private readonly string _searchPlaceholder = "Search...";

        private int _selectedIndex = -1;
        private Vector2 _scrollPos;
        private List<string> _lastItems;

        private Action<int> _onRowAdd;
        private Action<int> _onRowRemove;
        private Action<int> _onRowSelect;
        private Action<int> _onRowEdit;
        private Action<int, int> _onRowMove;
        private Action<int> _onRowDragOut;
        private Action<int> _onRowReceiveDrop;
        private Action<int> _onButtonClick;

        private readonly object _reorderToken = new();
        private int _reorderControlId;
        private int _pressDataIndex = -1;
        private Vector2 _pressPos;
        private int _reorderFromIndex = -1;
        private int _reorderInsertIndex = -1;
        private bool _listReorderPromotedOut;

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

        private bool SelectRowCore(int index)
        {
            if (index < 0) return false;
            if (_lastItems != null && index >= _lastItems.Count) return false;
            _selectedIndex = index;
            _onRowSelect?.Invoke(index);
            return true;
        }

        private void DrawCore(Rect rect, List<string> items)
        {
            _lastItems = items;
            CheckRenameBlur();
            HandleKeyboard();
            if (items == null) return;

            if (_pendingBodyContextMenu) { _pendingBodyContextMenu = false; ShowBodyContextMenu(items); }
            if (_hasPendingContextMenu) { _hasPendingContextMenu = false; ShowContextMenu(items, _pendingContextIndex, _pendingContextLabel); _pendingContextLabel = null; }

            if (_pendingBeginRename && _selectedIndex >= 0 && _selectedIndex < items.Count)
            { BeginRename(items, _selectedIndex, items[_selectedIndex]); _pendingBeginRename = false; }

            if (_pendingDelete && _selectedIndex >= 0 && _selectedIndex < items.Count)
            { TryRemoveRowAt(items, _selectedIndex); _pendingDelete = false; }

            var boxRect = BoxDrawer.CalcBoxRect(rect);
            if (boxRect.width < 1f || boxRect.height < 1f) return;

            BoxDrawer.DrawBox(boxRect);
            var contentRect = BoxDrawer.CalcContentRect(boxRect);
            GUI.BeginGroup(contentRect);

            var tbH = ShowToolbar ? ControlsToolbar.ToolbarHeight : 0f;
            if (ShowToolbar) DrawToolbar(new Rect(0f, 0f, contentRect.width, tbH));
            else _searchText = string.Empty;

            if (_selectedIndex >= items.Count) { _selectedIndex = items.Count - 1; GUI.changed = true; }
            if (_renamingIndex >= 0 && _renamingIndex >= items.Count) CommitRename(false);

            DrawBody(new Rect(0f, tbH, contentRect.width, Mathf.Max(0f, contentRect.height - tbH)), items, GetFilteredIndices(items));
            GUI.EndGroup();
        }

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
            _onRowEdit?.Invoke(idx);
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
            _onRowRemove?.Invoke(index);
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
            if (Event.current.keyCode == KeyCode.F2 && CanEdit) { _pendingBeginRename = true; Event.current.Use(); }
            if (Event.current.keyCode == KeyCode.Delete && CanRemove) { _pendingDelete = true; Event.current.Use(); }
        }
    }
}
