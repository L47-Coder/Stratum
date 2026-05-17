using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stratum;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TableControl
    {
        private void DrawToolbar(Rect toolbarRect)
        {
            ControlsToolbar.DrawToolbarSeparator(toolbarRect);

            var buttons = ToolbarButtons;
            var hasButtons = buttons != null && buttons.Count > 0;
            var btnSize = ControlsToolbar.SearchFieldHeight;
            var spacing = ControlsToolbar.ToolbarButtonSpacing;
            float searchXMax = toolbarRect.xMax;

            if (hasButtons)
            {
                var stripW = buttons.Count * btnSize + (buttons.Count - 1) * spacing;
                var x0 = toolbarRect.xMax - stripW;
                searchXMax = x0 - ControlsToolbar.ToolbarSectionGap;

                for (var i = 0; i < buttons.Count; i++)
                {
                    var content = buttons[i] ?? new GUIContent($"{i + 1}", "Empty button");
                    var r = new Rect(
                        x0 + i * (btnSize + spacing),
                        toolbarRect.y + (toolbarRect.height - btnSize) * 0.5f,
                        btnSize,
                        btnSize);
                    var style = content.image != null ? EditorStyles.iconButton : ControlsToolbar.ButtonStyle;
                    if (GUI.Button(r, content, style))
                    {
                        GUI.FocusControl(null);
                        _onButtonClick?.Invoke(i);
                    }
                }
            }

            var searchW = Mathf.Max(searchXMax - toolbarRect.x, 20f);
            DrawSearchBar(new Rect(toolbarRect.x, toolbarRect.y, searchW, toolbarRect.height));
        }

        private void DrawHeader<T>(Rect rowRect, List<T> list, TableLayout layout)
        {
            var contentWidth = Mathf.Max(rowRect.width, layout.TotalWidth);
            var viewRect = new Rect(0f, 0f, contentWidth, rowRect.height);
            var headerScroll = new Vector2(_scrollPos.x, 0f);
            GUI.BeginScrollView(rowRect, headerScroll, viewRect, GUIStyle.none, GUIStyle.none);

            var innerRect = new Rect(0f, 0f, contentWidth, rowRect.height);
            var cursorX = 0f;

            {
                var indexRect = new Rect(cursorX, innerRect.y, layout.IndexWidth, innerRect.height);
                PaintCellFrame(indexRect, HeaderCellBackground, GridLineColor);
                GUI.Label(PaddedRect(indexRect), list.Count.ToString(), HeaderCellLabelStyle);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                    indexRect.Contains(Event.current.mousePosition))
                {
                    _selectedIndex = -1;
                    RequestGuiVisualRefresh();
                    Event.current.Use();
                }
                cursorX = indexRect.xMax;
            }

            for (var i = 0; i < _columns.Count; i++)
            {
                var cell = new Rect(cursorX, innerRect.y, layout.DataColumnWidths[i], innerRect.height);
                PaintCellFrame(cell, HeaderCellBackground, GridLineColor);
                GUI.Label(PaddedRect(cell), _columns[i].Title, HeaderCellLabelStyle);

                HandleColumnResize(cell, innerRect, layout, i);

                cursorX = cell.xMax;
            }

            if (layout.ActionsWidth > 0)
            {
                var cellWidth = ActionCellWidth;
                var actionX = innerRect.xMax - layout.ActionsWidth;

                for (var i = 0; i < RowButtonCount; i++)
                {
                    var cell = new Rect(actionX, innerRect.y, cellWidth, innerRect.height);
                    PaintCellFrame(cell, HeaderCellBackground, GridLineColor);
                    var headerContent = RowButtons[i];
                    if (headerContent != null && (headerContent.image != null || !string.IsNullOrEmpty(headerContent.text)))
                    {
                        var oldColor = GUI.color;
                        GUI.color = new Color(1f, 1f, 1f, 0.75f);
                        GUI.Label(PaddedRect(cell), headerContent, RowButtonHeaderStyle);
                        GUI.color = oldColor;
                    }
                    actionX += cellWidth;
                }

                if (HasAddRemoveCell)
                {
                    var addRect = new Rect(actionX, innerRect.y, cellWidth, innerRect.height);
                    PaintCellFrame(addRect, HeaderCellBackground, GridLineColor);
                    using (new EditorGUI.DisabledScope(!CanAdd))
                    {
                        if (GUI.Button(PaddedRect(addRect), "＋"))
                        {
                            GUI.FocusControl(null);
                            try { list.Add(Activator.CreateInstance<T>()); }
                            catch { list.Add(default); }
                            var newIndex = list.Count - 1;
                            _onRowAdd?.Invoke(newIndex);
                            _selectedIndex = -1;
                            GUI.changed = true;
                        }
                    }
                }
            }

            GUI.EndScrollView();
        }

        private void HandleColumnResize(Rect cell, Rect rowRect, TableLayout layout, int columnIndex)
        {
            var splitterRect = new Rect(cell.xMax - 3f, rowRect.y, 6f, rowRect.height);
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.SplitResizeLeftRight);

            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            var e = Event.current;
            switch (e.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && splitterRect.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = controlId;
                        _resizeColumnIndex = columnIndex;
                        _resizeLastMouseX = e.mousePosition.x;
                        for (var j = 0; j < _columns.Count; j++)
                            _columnPreferredWidths[j] = Mathf.Max(_columnMinWidths[j], layout.DataColumnWidths[j]);
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        var deltaFrame = e.mousePosition.x - _resizeLastMouseX;
                        _resizeLastMouseX = e.mousePosition.x;
                        var lastIdx = _columns.Count - 1;
                        if (lastIdx >= 0 && !Mathf.Approximately(deltaFrame, 0f))
                        {
                            if (_resizeColumnIndex < lastIdx)
                            {
                                var k = _resizeColumnIndex;
                                var curK = _columnPreferredWidths[k];
                                var newK = Mathf.Max(_columnMinWidths[k], curK + deltaFrame);
                                var dK = newK - curK;

                                if (dK > 0f)
                                {
                                    var room = _columnPreferredWidths[lastIdx] - _columnMinWidths[lastIdx];
                                    var take = Mathf.Min(dK, room);
                                    _columnPreferredWidths[k] += take;
                                    _columnPreferredWidths[lastIdx] -= take;
                                    var rest = dK - take;
                                    if (rest > 0f)
                                        _columnPreferredWidths[k] += rest;
                                }
                                else if (dK < 0f)
                                {
                                    _columnPreferredWidths[k] += dK;
                                    if (!layout.NeedsHorizontalScroll)
                                        _columnPreferredWidths[lastIdx] -= dK;
                                }
                            }
                            else
                            {
                                _columnPreferredWidths[lastIdx] = Mathf.Max(
                                    _columnMinWidths[lastIdx],
                                    _columnPreferredWidths[lastIdx] + deltaFrame);
                            }
                        }
                        RequestGuiVisualRefresh();
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        _resizeColumnIndex = -1;
                        e.Use();
                    }
                    break;
            }
        }

        private void DrawRows<T>(Rect bodyRect, List<T> list, List<int> filteredIndices, TableLayout layout, float viewWidth)
        {
            var isSearching = ShowToolbar && !string.IsNullOrWhiteSpace(_searchText);
            var rowCount = list.Count;
            var displayCount = filteredIndices.Count;
            var rowHeight = ComputeRowHeight();
            var uniformHeights = BuildUniformHeights(displayCount, rowHeight);
            var invalidIndices = BuildDuplicateIndices(list);

            if (isSearching && _draggingOwner == this)
                EndReorderSession();

            if (isSearching && _rowIndexInteractDown)
            {
                if (GUIUtility.hotControl == _rowIndexInteractControlId)
                    GUIUtility.hotControl = 0;
                ClearRowIndexInteractAfterReorderOrCancel();
            }

            var rowsContentWidth = Mathf.Max(viewWidth, layout.TotalWidth);
            var totalH = displayCount * rowHeight;
            var viewRect = new Rect(0f, 0f, rowsContentWidth, totalH);

            if (CanReceiveDrop)
                HandleTableReceiveDrop(bodyRect, filteredIndices, rowHeight);

            _scrollPos = GUI.BeginScrollView(bodyRect, _scrollPos, viewRect);

            var inner = new Rect(0f, 0f, rowsContentWidth, viewRect.height);

            OnRowIndexInteractIgnore();
            FlushRowIndexInteractClickSelect(list);

            var isDraggingThis = (CanReorder || CanDragOut) && !isSearching &&
                                 _draggingOwner == this && _reorder != null &&
                                 _reorder.ArraySize == rowCount;

            if (isDraggingThis)
            {
                EnsureSessionBuffers(rowCount);
                if (Event.current.type == EventType.Repaint)
                {
                    if (!_reorder.PositionsInitialized)
                        InitializeDragPositions(inner.yMin, uniformHeights);
                    ApplyBodyScrollDelta(inner.yMin);
                    UpdateInsertSlotFromMouse(inner, uniformHeights, rowCount);
                    UpdateTargets(inner.yMin, uniformHeights);
                    StepSessionAnimation(BeginFrameDelta());
                }
            }
            else if (_draggingOwner == this && _reorder != null && _reorder.ArraySize != rowCount)
            {
                EndReorderSession();
            }

            var visualRows = BuildVisualRows(inner, uniformHeights, filteredIndices, isDraggingThis);
            var removeIndex = -1;

            foreach (var visual in visualRows.OrderBy(v => v.DrawY))
            {
                var rowRect = new Rect(inner.x, visual.DrawY, inner.width, visual.Height);
                if (visual.IsGap)
                {
                    DrawGapPlaceholder(rowRect);
                    continue;
                }
                var isInvalid = invalidIndices != null && invalidIndices.Contains(visual.RowIndex);
                var isDrop = CanReceiveDrop && _dropHighlightDataIndex >= 0 && visual.RowIndex == _dropHighlightDataIndex;
                DrawRow(rowRect, list, visual.RowIndex, visual.StripeIndex, isSearching, ref removeIndex, layout, isInvalid: isInvalid, isDrop: isDrop);
            }

            if (isDraggingThis && _reorder != null)
            {
                var floaterStripe = 0;
                for (var i = 0; i < filteredIndices.Count; i++)
                    if (filteredIndices[i] == _reorder.SourceIndex) { floaterStripe = i % 2; break; }

                var floatRect = new Rect(inner.x, _reorder.DragRowY, inner.width, _reorder.SourceRowHeight);
                DrawDragFloatingRowShadow(floatRect);
                var floatInvalid = invalidIndices != null && invalidIndices.Contains(_reorder.SourceIndex);
                DrawRow(floatRect, list, _reorder.SourceIndex, floaterStripe, isSearching, ref removeIndex, layout, isDragFloating: true, isInvalid: floatInvalid);
            }

            if ((CanReorder || CanDragOut) && !isSearching)
                HandleActiveReorderLifecycle(list);

            GUI.EndScrollView();

            if (removeIndex >= 0)
            {
                if (_draggingOwner == this && _reorder?.SourceIndex == removeIndex)
                    EndReorderSession();
                var beforeCount = list.Count;
                _onRowRemove?.Invoke(removeIndex);
                if (list.Count == beforeCount && removeIndex < list.Count)
                    list.RemoveAt(removeIndex);
                if (_selectedIndex == removeIndex) _selectedIndex = -1;
                else if (_selectedIndex > removeIndex) _selectedIndex--;
                if (_selectedIndex >= list.Count) _selectedIndex = -1;
                GUI.changed = true;
            }
        }

        private static float[] BuildUniformHeights(int count, float height)
        {
            var arr = new float[count];
            for (var i = 0; i < count; i++) arr[i] = height;
            return arr;
        }

        private void DrawRow<T>(
            Rect rowRect,
            List<T> list,
            int dataIndex,
            int stripeIndex,
            bool isSearching,
            ref int removeIndex,
            TableLayout layout,
            bool isDragFloating = false,
            bool isInvalid = false,
            bool isDrop = false)
        {
            var isSelected = !isDragFloating && _selectedIndex == dataIndex;
            var fill = isInvalid
                ? (EditorGUIUtility.isProSkin
                    ? new Color(0.55f, 0.26f, 0.26f, 1f)
                    : new Color(1.00f, 0.82f, 0.82f, 1f))
                : isDrop
                    ? DropTargetCellBackground
                : isSelected
                    ? SelectedCellBackground
                    : BodyCellBackground(stripeIndex % 2 == 1);

            var rowControlId = GUIUtility.GetControlID(
                RowReorderControlHintHash ^ GetHashCode() ^ (dataIndex * 7919), FocusType.Passive);

            var cursorX = rowRect.x;

            var indexRect = new Rect(cursorX, rowRect.y, layout.IndexWidth, rowRect.height);
            PaintCellFrame(indexRect, fill, GridLineColor);
            GUI.Label(PaddedRect(indexRect), $"{dataIndex}", BodyIndexLabelStyle);

            if (!isDragFloating)
            {
                var e = Event.current;
                if ((CanReorder || CanDragOut) && !isSearching)
                {
                    if (e.type == EventType.MouseDown && e.button == 0 && indexRect.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = rowControlId;
                        _rowIndexInteractDown = true;
                        _rowIndexInteractControlId = rowControlId;
                        _rowIndexInteractDataIndex = dataIndex;
                        _rowIndexInteractPressPos = e.mousePosition;
                        _rowIndexInteractPressTime = EditorApplication.timeSinceStartup;
                        _rowIndexInteractRowRect = rowRect;
                        _rowIndexInteractRowHeight = rowRect.height;
                        _rowIndexInteractListCount = list.Count;
                        _rowIndexInteractSearchingAtPress = false;
                        CaptureRowIndexPendingForHold();
                        GUI.FocusControl(null);
                        e.Use();
                    }
                    TryPromoteRowIndexToDrag(rowRect, dataIndex, list.Count, rowRect.height, rowControlId);
                }
                else
                {
                    HandleRowSelectInput(indexRect, dataIndex, isDragFloating);
                }
            }
            cursorX = indexRect.xMax;

            for (var i = 0; i < _columns.Count; i++)
            {
                var cell = new Rect(cursorX, rowRect.y, layout.DataColumnWidths[i], rowRect.height);
                var field = _columns[i].Field;
                PaintCellFrame(cell, fill, GridLineColor);

                if (field == null)
                    EditorGUI.LabelField(PaddedRect(cell), $"Missing field: {_columns[i].RelativePropertyPath}", EditorStyles.wordWrappedMiniLabel);
                else
                {
                    using (new EditorGUI.DisabledScope(_columns[i].Readonly || !CanEdit || isDragFloating))
                        DrawCellField(PaddedRect(cell), list, dataIndex, field);
                }
                cursorX = cell.xMax;
            }

            if (layout.ActionsWidth > 0)
            {
                var cellWidth = ActionCellWidth;
                var actionX = rowRect.xMax - layout.ActionsWidth;
                var buttons = RowButtons;

                for (var i = 0; i < RowButtonCount; i++)
                {
                    var cell = new Rect(actionX, rowRect.y, cellWidth, rowRect.height);
                    PaintCellFrame(cell, fill, GridLineColor);
                    var content = buttons[i] ?? new GUIContent($"{i + 1}", "Empty button");
                    var style = content.image != null ? RowIconButtonStyle : RowTextButtonStyle;
                    using (new EditorGUI.DisabledScope(isDragFloating))
                    {
                        if (GUI.Button(PaddedRect(cell), content, style))
                        {
                            GUI.FocusControl(null);
                            _onRowButtonClick?.Invoke(dataIndex, i);
                        }
                    }
                    actionX += cellWidth;
                }

                if (HasAddRemoveCell)
                {
                    var removeRect = new Rect(actionX, rowRect.y, cellWidth, rowRect.height);
                    PaintCellFrame(removeRect, fill, GridLineColor);
                    using (new EditorGUI.DisabledScope(isDragFloating || !CanRemove))
                    {
                        if (GUI.Button(PaddedRect(removeRect), "−"))
                        {
                            GUI.FocusControl(null);
                            removeIndex = dataIndex;
                        }
                    }
                }
            }
        }

        private void HandleRowSelectInput(Rect indexRect, int dataIndex, bool isDragFloating)
        {
            if (isDragFloating || _draggingOwner == this || _rowIndexInteractDown) return;
            if (Event.current.type != EventType.MouseDown || Event.current.button != 0) return;
            if (!indexRect.Contains(Event.current.mousePosition)) return;
            if (_selectedIndex == dataIndex)
                _selectedIndex = -1;
            else if (!CanSelect)
                return;
            else
            {
                _selectedIndex = dataIndex;
                _onRowSelect?.Invoke(dataIndex);
            }

            GUI.FocusControl(null);
            Event.current.Use();
            RequestGuiVisualRefresh();
        }

        private void DrawCellField<T>(Rect rect, List<T> list, int index, FieldInfo field)
        {
            var boxed = (object)list[index];
            var value = field.GetValue(boxed);

            var opts = new FieldDrawer.Options
            {
                DelayedNumeric = true,
                UnfocusOnMouseDown = true,
                UnsupportedLabelStyle = EditorStyles.miniLabel,
            };

            if (!FieldDrawer.TryDraw(rect, field, value, in opts, out var newValue)) return;
            field.SetValue(boxed, newValue);
            list[index] = (T)boxed;
            GUI.changed = true;
            _onRowEdit?.Invoke(index);
        }

        private static void DrawDragFloatingRowShadow(Rect rowRect)
        {
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(new Rect(rowRect.x + 2f, rowRect.y + 3f, rowRect.width, rowRect.height), new Color(0f, 0f, 0f, 0.18f));
            DrawRectOutline(rowRect, new Color(0.28f, 0.58f, 0.98f, 0.55f), 1f);
        }

        private static void DrawGapPlaceholder(Rect gap)
        {
            if (Event.current.type != EventType.Repaint) return;
            var pulse = 0.55f + 0.45f * Mathf.Sin((float)EditorApplication.timeSinceStartup * 6f);
            EditorGUI.DrawRect(gap, new Color(0.25f, 0.55f, 0.95f, 0.11f + 0.08f * pulse));
            DrawRectOutline(gap, new Color(0.32f, 0.62f, 1f, 0.28f + 0.12f * pulse), 1f);
        }

        private void HandleTableReceiveDrop(Rect bodyRect, List<int> filteredIndices, float rowHeight)
        {
            if (!CanReceiveDrop)
            {
                if (Event.current.type == EventType.DragExited) _dropHighlightDataIndex = -1;
                return;
            }

            var e = Event.current;
            if (e.type == EventType.DragExited) { _dropHighlightDataIndex = -1; return; }
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;

            if (!bodyRect.Contains(e.mousePosition))
            {
                _dropHighlightDataIndex = -1;
                if (e.type == EventType.DragUpdated) DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                return;
            }

            var vi = Mathf.FloorToInt((e.mousePosition.y - bodyRect.y + _scrollPos.y) / rowHeight);
            var dataIndex = vi >= 0 && vi < filteredIndices.Count ? filteredIndices[vi] : -1;

            if (e.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                _dropHighlightDataIndex = dataIndex;
                GUI.changed = true;
                e.Use();
            }
            else
            {
                DragAndDrop.AcceptDrag();
                _dropHighlightDataIndex = -1;
                _onRowReceiveDrop?.Invoke(dataIndex);
                GUI.changed = true;
                e.Use();
            }
        }

        private HashSet<int> BuildDuplicateIndices<T>(List<T> list)
        {
            if (!MarkDuplicates) return null;
            var col = FindKeyColumn();
            if (!col.HasValue || col.Value.Field == null) return null;

            var field = col.Value.Field;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var dupes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in list)
            {
                var k = NormalizeDuplicateCompareKey(field.GetValue(item), field.FieldType);
                if (!string.IsNullOrEmpty(k) && !seen.Add(k)) dupes.Add(k);
            }
            if (dupes.Count == 0) return null;

            var result = new HashSet<int>();
            for (var i = 0; i < list.Count; i++)
            {
                var k = NormalizeDuplicateCompareKey(field.GetValue(list[i]), field.FieldType);
                if (!string.IsNullOrEmpty(k) && dupes.Contains(k)) result.Add(i);
            }
            return result;
        }
    }
}
