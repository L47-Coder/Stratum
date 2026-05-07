using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class ListControl
    {
        private void DrawToolbar(Rect r)
        {
            ControlsToolbar.DrawToolbarSeparator(r);
            var pad = r.height - ControlsToolbar.ToolbarSeparatorHeight - ControlsToolbar.SearchFieldHeight;
            var left = r.x;
            var xMax = r.xMax - pad;
            var btnSize = ControlsToolbar.SearchFieldHeight;
            var spacing = ControlsToolbar.ToolbarButtonSpacing;
            var btns = ToolbarButtons;

            if (btns != null && btns.Count > 0)
            {
                var stripW = btns.Count * btnSize + (btns.Count - 1) * spacing;
                var x0 = xMax - stripW;
                for (var i = 0; i < btns.Count; i++)
                {
                    var content = btns[i] ?? new GUIContent($"{i + 1}", "Empty button");
                    var br = new Rect(x0 + i * (btnSize + spacing), r.y + (r.height - btnSize) * 0.5f, btnSize, btnSize);
                    var style = content.image != null ? EditorStyles.iconButton : ControlsToolbar.ButtonStyle;
                    if (GUI.Button(br, content, style)) { GUI.FocusControl(null); _onButtonClick?.Invoke(i); }
                }
                xMax = x0 - ControlsToolbar.ToolbarSectionGap;
            }
            DrawSearchBar(new Rect(left, r.y, Mathf.Max(xMax - left, 20f), r.height));
        }

        private static GUIStyle _labelStyle;
        private static GUIStyle _labelStyleSelected;
        private static GUIStyle LabelStyle => _labelStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
        private static GUIStyle LabelStyleSelected => _labelStyleSelected ??= new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft };

        private static Color RowBgColor(bool selected, bool alt, bool drop)
        {
            if (selected) return EditorGUIUtility.isProSkin ? new Color(0.17f, 0.36f, 0.53f, 1f) : new Color(0.24f, 0.49f, 0.91f, 1f);
            if (drop) return EditorGUIUtility.isProSkin ? new Color(0.20f, 0.40f, 0.62f, 0.55f) : new Color(0.24f, 0.49f, 0.91f, 0.30f);
            return alt
                ? (EditorGUIUtility.isProSkin ? new Color(0.15f, 0.15f, 0.16f, 1f) : new Color(0.96f, 0.97f, 0.98f, 1f))
                : (EditorGUIUtility.isProSkin ? new Color(0.13f, 0.13f, 0.14f, 1f) : new Color(1f, 1f, 1f, 1f));
        }

        private int _dropHighlightIndex = -1;

        private void DrawBody(Rect bodyRect, List<string> items, List<int> filtered)
        {
            var totalH = filtered.Count * RowHeight;
            var needScroll = totalH > bodyRect.height;
            var innerW = Mathf.Max(bodyRect.width - (needScroll ? ControlsToolbar.VerticalScrollbarWidth : 0f), 1f);
            var viewRect = new Rect(0f, 0f, innerW, Mathf.Max(totalH, bodyRect.height));

            if (CanReorder || CanDragOut) TryStartReorderDrag(bodyRect, filtered);
            if (CanReorder) HandleReorderDrag(bodyRect, items, filtered);
            if (CanReceiveDrop) HandleGlobalDrop(bodyRect, filtered);

            _scrollPos = GUI.BeginScrollView(bodyRect, _scrollPos, viewRect);

            for (var vi = 0; vi < filtered.Count; vi++)
                DrawRow(new Rect(0f, vi * RowHeight, innerW, RowHeight), items[filtered[vi]], filtered[vi], vi);

            if (CanReorder && _reorderInsertIndex >= 0 && Event.current.type == EventType.Repaint)
                DrawReorderIndicator(innerW, filtered);

            if (_renamingIndex >= 0 && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                var hit = false;
                for (var i = 0; i < filtered.Count; i++)
                {
                    if (filtered[i] != _renamingIndex) continue;
                    var ry = i * RowHeight;
                    hit = Event.current.mousePosition.y >= ry && Event.current.mousePosition.y < ry + RowHeight;
                    break;
                }
                if (!hit) CommitRename(true);
            }

            var ctx = Event.current;
            if (ctx.type == EventType.ContextClick &&
                ctx.mousePosition.x >= 0f && ctx.mousePosition.x <= innerW &&
                ctx.mousePosition.y >= 0f && ctx.mousePosition.y < viewRect.height)
            {
                var band = Mathf.FloorToInt(ctx.mousePosition.y / RowHeight);
                if (band < 0 || band >= filtered.Count)
                {
                    if (CanAdd) { if (_renamingIndex >= 0) CommitRename(true); _pendingBodyContextMenu = true; ctx.Use(); }
                }
            }

            GUI.EndScrollView();
        }

        private void DrawRow(Rect rowRect, string label, int dataIndex, int vi)
        {
            var isSelected = dataIndex == _selectedIndex;
            var isDrop = CanReceiveDrop && dataIndex == _dropHighlightIndex;
            var isRenaming = _renamingIndex == dataIndex;

            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(rowRect, RowBgColor(isSelected, vi % 2 == 1, isDrop));

            var labelRect = new Rect(rowRect.x + 8f, rowRect.y, Mathf.Max(0f, rowRect.width - 10f), rowRect.height);

            if (isRenaming) { DrawRenameField(labelRect); return; }

            var prev = GUI.contentColor;
            if (isSelected) GUI.contentColor = Color.white;
            GUI.Label(labelRect, label ?? string.Empty, isSelected ? LabelStyleSelected : LabelStyle);
            GUI.contentColor = prev;

            if (Event.current.type == EventType.ContextClick && rowRect.Contains(Event.current.mousePosition))
            {
                if (_renamingIndex >= 0) CommitRename(true);
                if (CanSelect) { _selectedIndex = dataIndex; _onRowSelect?.Invoke(dataIndex); }
                if (CanAdd || CanEdit || CanRemove)
                { _hasPendingContextMenu = true; _pendingContextIndex = dataIndex; _pendingContextLabel = label; }
                GUI.changed = true; Event.current.Use();
                return;
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rowRect.Contains(Event.current.mousePosition))
            {
                if (_renamingIndex >= 0 && _renamingIndex != dataIndex) CommitRename(true);
                if (!CanSelect) return;
                _selectedIndex = dataIndex; _onRowSelect?.Invoke(dataIndex);
                GUI.changed = true; Event.current.Use();
            }
        }

        private void DrawRenameField(Rect rect)
        {
            var fieldH = EditorGUIUtility.singleLineHeight;
            rect = new Rect(rect.x, rect.y + (rect.height - fieldH) * 0.5f, rect.width, fieldH);
            var ctrl = RenameCtrl;
            var wantText = _renameBuffer ?? string.Empty;
            var hasFocus = GUI.GetNameOfFocusedControl() == ctrl;
            GUI.SetNextControlName(ctrl);

            if (_renameFocusRequest)
            {
                EditorGUI.FocusTextInControl(ctrl);
                EditorGUI.TextField(rect, wantText);
                if (hasFocus)
                {
                    if (GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) is TextEditor ed)
                    { ed.text = wantText; ed.SelectAll(); }
                    _renameFocusRequest = false;
                }
            }
            else
                _renameBuffer = EditorGUI.TextField(rect, wantText);

            if (hasFocus) _renameHadFocus = true;
        }

        private void TryAppendNewRow(List<string> items)
        {
            if (items == null || !CanAdd) return;
            GUI.FocusControl(null);
            items.Add("New");
            _selectedIndex = -1;
            _onRowAdd?.Invoke(items.Count - 1);
            GUI.changed = true;
        }

        private void ShowBodyContextMenu(List<string> items)
        {
            if (!CanAdd) return;
            GUI.FocusControl(null);
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent(_createLabel), false, () => TryAppendNewRow(items));
            menu.ShowAsContext();
        }

        private void ShowContextMenu(List<string> items, int dataIndex, string label)
        {
            if (!CanAdd && !CanEdit && !CanRemove) return;
            GUI.FocusControl(null);
            var menu = new GenericMenu();
            if (CanAdd) menu.AddItem(new GUIContent(_createLabel), false, () => TryAppendNewRow(items));
            if (CanEdit) menu.AddItem(new GUIContent(_renameLabel), false, () => BeginRename(items, dataIndex, label));
            if (CanRemove) menu.AddItem(new GUIContent(_deleteLabel), false, () => { if (items != null) TryRemoveRowAt(items, dataIndex); });
            menu.ShowAsContext();
        }

        private void HandleGlobalDrop(Rect bodyRect, List<int> filtered)
        {
            var e = Event.current;
            if (e.type == EventType.DragExited) { _dropHighlightIndex = -1; return; }
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;
            if (ReferenceEquals(DragAndDrop.GetGenericData(ReorderDragKey), _reorderToken)) return;

            if (!bodyRect.Contains(e.mousePosition))
            {
                _dropHighlightIndex = -1;
                if (e.type == EventType.DragUpdated) DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                return;
            }

            var vi = Mathf.FloorToInt((e.mousePosition.y - bodyRect.y + _scrollPos.y) / RowHeight);
            var dataIndex = vi >= 0 && vi < filtered.Count ? filtered[vi] : -1;

            if (e.type == EventType.DragUpdated)
            { DragAndDrop.visualMode = DragAndDropVisualMode.Move; _dropHighlightIndex = dataIndex; GUI.changed = true; e.Use(); }
            else
            { DragAndDrop.AcceptDrag(); _dropHighlightIndex = -1; _onRowReceiveDrop?.Invoke(dataIndex); GUI.changed = true; e.Use(); }
        }

        private void TryStartReorderDrag(Rect bodyRect, List<int> filtered)
        {
            var e = Event.current;
            _reorderControlId = GUIUtility.GetControlID(s_reorderControlHint, FocusType.Passive);

            switch (e.GetTypeForControl(_reorderControlId))
            {
                case EventType.MouseDown:
                    if (e.button != 0 || !bodyRect.Contains(e.mousePosition)) break;
                    var vi = Mathf.FloorToInt((e.mousePosition.y - bodyRect.y + _scrollPos.y) / RowHeight);
                    if (vi < 0 || vi >= filtered.Count) break;
                    _listReorderPromotedOut = false;
                    _pressDataIndex = filtered[vi]; _pressPos = e.mousePosition;
                    GUIUtility.hotControl = _reorderControlId;
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != _reorderControlId || _pressDataIndex < 0) break;
                    if (Vector2.Distance(e.mousePosition, _pressPos) < ReorderDragThreshold) break;
                    var dragIdx = _pressDataIndex;
                    _pressDataIndex = -1;
                    if (CanReorder)
                    {
                        _reorderFromIndex = dragIdx;
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.SetGenericData(ReorderDragKey, _reorderToken);
                        DragAndDrop.SetGenericData(ReorderFromKey, _reorderFromIndex);
                        DragAndDrop.objectReferences = System.Array.Empty<UnityEngine.Object>();
                        DragAndDrop.paths = System.Array.Empty<string>();
                        DragAndDrop.StartDrag("ListViewReorder");
                    }
                    else if (CanDragOut)
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = System.Array.Empty<UnityEngine.Object>();
                        DragAndDrop.paths = System.Array.Empty<string>();
                        _onRowDragOut?.Invoke(dragIdx);
                        DragAndDrop.StartDrag("Row");
                    }
                    GUIUtility.hotControl = 0; e.Use();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == _reorderControlId) GUIUtility.hotControl = 0;
                    _pressDataIndex = -1;
                    break;
            }
        }

        private void HandleReorderDrag(Rect bodyRect, List<string> items, List<int> filtered)
        {
            var e = Event.current;
            if (e.type == EventType.DragExited) { _reorderInsertIndex = -1; _listReorderPromotedOut = false; return; }
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;
            if (!ReferenceEquals(DragAndDrop.GetGenericData(ReorderDragKey), _reorderToken)) return;

            var inside = bodyRect.Contains(e.mousePosition);
            var insertIdx = items.Count;
            if (inside)
            {
                var insertVi = Mathf.Clamp(Mathf.RoundToInt((e.mousePosition.y - bodyRect.y + _scrollPos.y) / RowHeight), 0, filtered.Count);
                insertIdx = insertVi < filtered.Count ? filtered[insertVi] : items.Count;
            }

            if (e.type == EventType.DragUpdated)
            {
                if (!inside && CanDragOut && CanReorder && !_listReorderPromotedOut)
                {
                    var promoFromKey = DragAndDrop.GetGenericData(ReorderFromKey);
                    var fromPromo = promoFromKey is int fp ? fp : -1;
                    if (fromPromo >= 0)
                    {
                        _listReorderPromotedOut = true;
                        _onRowDragOut?.Invoke(fromPromo);
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = System.Array.Empty<UnityEngine.Object>();
                        DragAndDrop.paths = System.Array.Empty<string>();
                        DragAndDrop.StartDrag("Row");
                        _reorderInsertIndex = -1;
                        GUI.changed = true; e.Use(); return;
                    }
                }
                DragAndDrop.visualMode = inside ? DragAndDropVisualMode.Move : DragAndDropVisualMode.Rejected;
                _reorderInsertIndex = inside ? insertIdx : -1;
                GUI.changed = true; e.Use(); return;
            }

            DragAndDrop.AcceptDrag();
            var performFromKey = DragAndDrop.GetGenericData(ReorderFromKey);
            var from = performFromKey is int fi ? fi : _reorderFromIndex;
            _reorderInsertIndex = _reorderFromIndex = -1;
            _listReorderPromotedOut = false;

            if (inside && from >= 0 && from < items.Count && insertIdx != from && insertIdx != from + 1)
                ApplyInternalReorder(items, from, insertIdx);

            GUI.changed = true; e.Use();
        }

        private void ApplyInternalReorder(List<string> items, int from, int insertIdx)
        {
            var si = _selectedIndex;
            var item = items[from];
            items.RemoveAt(from);
            var ins = Mathf.Clamp(insertIdx > from ? insertIdx - 1 : insertIdx, 0, items.Count);

            var newSi = si < 0 ? -1
                : si == from ? ins
                : ins <= (si > from ? si - 1 : si) ? (si > from ? si - 1 : si) + 1
                : (si > from ? si - 1 : si);

            items.Insert(ins, item);
            _selectedIndex = si < 0 ? -1 : Mathf.Clamp(newSi, 0, items.Count - 1);
            _onRowMove?.Invoke(from, ins);
        }

        private void DrawReorderIndicator(float innerW, List<int> filtered)
        {
            var insertVi = filtered.Count;
            for (var vi = 0; vi < filtered.Count; vi++)
                if (filtered[vi] >= _reorderInsertIndex) { insertVi = vi; break; }

            const float inset = 8f, thick = 1f, bw = 2f, bh = 10f;
            var c = ControlsToolbar.DropIndicatorColor; c.a *= 0.92f;
            var lineY = insertVi * RowHeight - thick * 0.5f;
            var xb = Mathf.Min(inset, Mathf.Max(0f, innerW - bw));
            var bwCl = Mathf.Min(bw, innerW);
            var lineW = Mathf.Max(0f, innerW - xb - bwCl - inset);

            EditorGUI.DrawRect(new Rect(xb, lineY - (bh - thick) * 0.5f, bwCl, bh), c);
            if (lineW > 0f) EditorGUI.DrawRect(new Rect(xb + bwCl, lineY, lineW, thick), c);
        }
    }
}
