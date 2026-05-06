using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stratum;
using UnityEditor;
using UnityEditorInternal;
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
                        _onButtonClicked?.Invoke(i);
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
                if (CanSelect && Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
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
                var actionRect = new Rect(innerRect.xMax - layout.ActionsWidth, innerRect.y, layout.ActionsWidth, innerRect.height);
                PaintCellFrame(actionRect, HeaderCellBackground, GridLineColor);
                using (new EditorGUI.DisabledScope(!CanAdd))
                {
                    if (GUI.Button(PaddedRect(actionRect), "＋"))
                    {
                        GUI.FocusControl(null);
                        try { list.Add(Activator.CreateInstance<T>()); }
                        catch { list.Add(default); }
                        var newIndex = list.Count - 1;
                        _onRowAdded?.Invoke(newIndex);
                        _selectedIndex = -1;
                        GUI.changed = true;
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
                        GUI.changed = true;
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

            _scrollPos = GUI.BeginScrollView(bodyRect, _scrollPos, viewRect);

            var inner = new Rect(0f, 0f, rowsContentWidth, viewRect.height);

            OnRowIndexInteractIgnore();
            FlushRowIndexInteractClickSelect(list);

            var isDraggingThis = CanDrag && !isSearching &&
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
                DrawRow(rowRect, list, visual.RowIndex, visual.StripeIndex, isSearching, ref removeIndex, layout, isInvalid: isInvalid);
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

            if (CanDrag && !isSearching)
                HandleActiveReorderLifecycle(list);

            GUI.EndScrollView();

            if (removeIndex >= 0)
            {
                if (_draggingOwner == this && _reorder?.SourceIndex == removeIndex)
                    EndReorderSession();
                _onRowRemoved?.Invoke(removeIndex);
                list.RemoveAt(removeIndex);
                if (_selectedIndex == removeIndex) _selectedIndex = -1;
                else if (_selectedIndex > removeIndex) _selectedIndex--;
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
            bool isInvalid = false)
        {
            var isSelected = CanSelect && !isDragFloating && _selectedIndex == dataIndex;
            var fill = isInvalid
                ? (EditorGUIUtility.isProSkin
                    ? new Color(0.55f, 0.26f, 0.26f, 1f)
                    : new Color(1.00f, 0.82f, 0.82f, 1f))
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
                if (CanDrag && !isSearching)
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
                    using (new EditorGUI.DisabledScope(_columns[i].Readonly || !CanRename || isDragFloating))
                        DrawCellField(PaddedRect(cell), list, dataIndex, field, _columns[i].Dropdown);
                }
                cursorX = cell.xMax;
            }

            if (layout.ActionsWidth > 0)
            {
                var actionRect = new Rect(rowRect.xMax - layout.ActionsWidth, rowRect.y, layout.ActionsWidth, rowRect.height);
                PaintCellFrame(actionRect, fill, GridLineColor);
                using (new EditorGUI.DisabledScope(isDragFloating || !CanRemove))
                {
                    if (GUI.Button(PaddedRect(actionRect), "−"))
                    {
                        GUI.FocusControl(null);
                        removeIndex = dataIndex;
                    }
                }
            }
        }

        private void HandleRowSelectInput(Rect indexRect, int dataIndex, bool isDragFloating)
        {
            if (!CanSelect || isDragFloating || _draggingOwner == this || _rowIndexInteractDown) return;
            if (Event.current.type != EventType.MouseDown || Event.current.button != 0) return;
            if (!indexRect.Contains(Event.current.mousePosition)) return;
            if (_selectedIndex == dataIndex)
                _selectedIndex = -1;
            else
            {
                _selectedIndex = dataIndex;
                _onRowSelected?.Invoke(dataIndex);
            }
            GUI.FocusControl(null);
            Event.current.Use();
            RequestGuiVisualRefresh();
        }

        private void DrawCellField<T>(Rect rect, List<T> list, int index, FieldInfo field, DropdownAttribute dropdown)
        {
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                GUI.FocusControl(null);

            var boxed = (object)list[index];
            var value = field.GetValue(boxed);
            var type = field.FieldType;

            if (IsStringList(type))
            {
                if (value is not List<string> stringList)
                {
                    stringList = new List<string>();
                    field.SetValue(boxed, stringList);
                    list[index] = (T)boxed;
                    GUI.changed = true;
                    _onRowRenamed?.Invoke(index);
                }
                DrawStringListCell(rect, stringList, index);
                return;
            }

            // IFieldExpandable：渲染展开按钮，点击触发 OnExpandFieldAt 回调
            if (typeof(IFieldExpandable).IsAssignableFrom(type))
            {
                DrawExpandableCell(rect, value, index, field.Name);
                return;
            }

            var isSupported =
                type == typeof(string) || type == typeof(int) || type == typeof(float) ||
                type == typeof(bool) || type.IsEnum ||
                typeof(UnityEngine.Object).IsAssignableFrom(type) ||
                type == typeof(AnimationCurve) || type == typeof(Gradient) ||
                type == typeof(Color) ||
                type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
                type == typeof(Vector2Int) || type == typeof(Vector3Int) ||
                type == typeof(Quaternion) ||
                type == typeof(LayerMask);

            if (!isSupported)
            {
                EditorGUI.LabelField(rect, value?.ToString() ?? "null", EditorStyles.miniLabel);
                return;
            }

            EditorGUI.BeginChangeCheck();
            object newValue;

            if (type == typeof(string))
            {
                if (dropdown != null)
                {
                    var cur = value as string ?? string.Empty;
                    var displayLabel = string.IsNullOrEmpty(cur) ? "(未选择)" : cur;
                    if (GUI.Button(rect, displayLabel, DropdownButtonStyle))
                    {
                        GUI.FocusControl(null);
                        var options = InvokeDropdownMethod(field, dropdown.Method);
                        if (options is { Length: > 0 })
                        {
                            var capturedField = field;
                            var capturedList  = list;
                            var capturedIndex = index;
                            var popup = new DropdownPopup
                            {
                                Multi     = dropdown.Multi,
                                Separator = dropdown.Separator,
                            };
                            popup.OnConfirmed(finalValue =>
                            {
                                var b = (object)capturedList[capturedIndex];
                                capturedField.SetValue(b, finalValue);
                                capturedList[capturedIndex] = (T)b;
                                _pendingDirty = true;
                                _onRowRenamed?.Invoke(capturedIndex);
                            });
                            popup.Show(rect, options, cur);
                        }
                    }
                    // DropdownPopup 通过回调写值，不走 EndChangeCheck 流程
                    newValue = cur;
                }
                else
                    newValue = EditorGUI.DelayedTextField(rect, value as string ?? string.Empty);
            }
            else if (type == typeof(int))
                newValue = EditorGUI.DelayedIntField(rect, value is int iv ? iv : 0);
            else if (type == typeof(float))
                newValue = EditorGUI.DelayedFloatField(rect, value is float fv ? fv : 0f);
            else if (type == typeof(bool))
                newValue = DrawToggleCell(rect, value is bool bv && bv);
            else if (type.IsEnum)
                newValue = EditorGUI.EnumPopup(rect, (Enum)value);
            else if (type == typeof(AnimationCurve))
                newValue = EditorGUI.CurveField(rect, value as AnimationCurve ?? new AnimationCurve());
            else if (type == typeof(Gradient))
                newValue = EditorGUI.GradientField(rect, value as Gradient ?? new Gradient());
            else if (type == typeof(Color))
                newValue = EditorGUI.ColorField(rect, value is Color cv ? cv : Color.white);
            else if (type == typeof(Vector2))
                newValue = EditorGUI.Vector2Field(rect, GUIContent.none, value is Vector2 v2 ? v2 : default);
            else if (type == typeof(Vector3))
                newValue = EditorGUI.Vector3Field(rect, GUIContent.none, value is Vector3 v3 ? v3 : default);
            else if (type == typeof(Vector4))
                newValue = EditorGUI.Vector4Field(rect, GUIContent.none, value is Vector4 v4 ? v4 : default);
            else if (type == typeof(Vector2Int))
                newValue = EditorGUI.Vector2IntField(rect, GUIContent.none, value is Vector2Int vi2 ? vi2 : default);
            else if (type == typeof(Vector3Int))
                newValue = EditorGUI.Vector3IntField(rect, GUIContent.none, value is Vector3Int vi3 ? vi3 : default);
            else if (type == typeof(Quaternion))
            {
                var q = value is Quaternion qv ? qv : Quaternion.identity;
                var euler = EditorGUI.Vector3Field(rect, GUIContent.none, q.eulerAngles);
                newValue = Quaternion.Euler(euler);
            }
            else if (type == typeof(LayerMask))
            {
                var mask = value is LayerMask lm ? lm : new LayerMask();
                var concatenated = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(mask);
                var picked = EditorGUI.MaskField(rect, concatenated, InternalEditorUtility.layers);
                newValue = (LayerMask)(int)InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(picked);
            }
            else
                newValue = EditorGUI.ObjectField(rect, value as UnityEngine.Object, type, true);

            if (EditorGUI.EndChangeCheck())
            {
                field.SetValue(boxed, newValue);
                list[index] = (T)boxed;
                GUI.changed = true;
                _onRowRenamed?.Invoke(index);
            }
        }

        private static readonly Color ToggleOnColor = new(0.22f, 0.62f, 0.35f, 0.88f);
        private static readonly Color ToggleOffColor = new(0.72f, 0.22f, 0.22f, 0.88f);

        private static GUIStyle _dropdownButtonStyle;
        private static GUIStyle DropdownButtonStyle =>
            _dropdownButtonStyle ??= new GUIStyle(EditorStyles.popup)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping  = TextClipping.Clip,
            };

        private static GUIStyle _toggleLabelStyle;
        private static GUIStyle ToggleLabelStyle => _toggleLabelStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Bold,
        };

        private static bool DrawToggleCell(Rect rect, bool current)
        {
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, current ? ToggleOnColor : ToggleOffColor);
                GUI.Label(rect, current ? "✓" : "✕", ToggleLabelStyle);
            }

            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
            {
                GUI.changed = true;
                Event.current.Use();
                return !current;
            }

            return current;
        }

        private static GUIStyle _expandableLabelStyle;
        private static GUIStyle ExpandableLabelStyle =>
            _expandableLabelStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(4, 4, 0, 0),
            };

        private void DrawExpandableCell(Rect rect, object value, int rowIndex, string fieldName)
        {
            var typeName = value == null ? "(null)" : value.GetType().Name;
            var e = Event.current;
            var isHover = rect.Contains(e.mousePosition);

            // 鼠标变成手型，强化“可点击链接”的心理预期
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            if (e.type == EventType.Repaint)
            {
                // 悬停时的微弱背景
                if (isHover)
                {
                    EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin
                        ? new Color(1f, 1f, 1f, 0.05f)
                        : new Color(0f, 0f, 0f, 0.05f));
                }

                // 链接文本颜色
                var linkColor = EditorGUIUtility.isProSkin
                    ? new Color(0.35f, 0.65f, 1f, 1f)
                    : new Color(0.1f, 0.4f, 0.8f, 1f);

                var oldColor = GUI.contentColor;
                GUI.contentColor = linkColor;

                // 绘制文本
                GUI.Label(rect, typeName, ExpandableLabelStyle);

                GUI.contentColor = oldColor;
            }

            if (e.type == EventType.MouseDown && e.button == 0 && isHover)
            {
                GUI.FocusControl(null);
                e.Use();
                _onExpandFieldAt?.Invoke(rowIndex, fieldName, rect);
            }

            if (isHover && e.type == EventType.MouseMove)
                RequestGuiVisualRefresh();
        }

        private static string[] InvokeDropdownMethod(FieldInfo field, string methodName)
        {
            if (_dropdownOptionsCache.TryGetValue(field, out var cached)) return cached;

            var method = field.DeclaringType?.GetMethod(
                methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            string[] result = null;
            if (method != null)
            {
                result = method.Invoke(null, null) switch
                {
                    string[] arr => arr,
                    List<string> list => list.ToArray(),
                    IEnumerable<string> e => e.ToArray(),
                    _ => null,
                };
            }

            // 无论成功与否都写入缓存，避免每帧重复反射
            _dropdownOptionsCache[field] = result ?? Array.Empty<string>();
            return result;
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
