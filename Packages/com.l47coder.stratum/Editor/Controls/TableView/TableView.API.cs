#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TableView
    {
        public bool CanAdd { get; set; } = true;
        public bool CanRemove { get; set; } = true;
        public bool CanSelect { get; set; } = true;
        public bool CanDrag { get; set; } = true;
        public bool CanRename { get; set; } = true;
        public bool ShowToolbar { get; set; } = true;
        public bool MarkDuplicates { get; set; } = true;
        public string KeyField { get; set; } = "Key";
        public List<GUIContent> ToolbarButtons { get; set; } = new();

        public void OnRowAdded(Action<int> callback) => _onRowAdded = callback;
        public void OnRowRemoved(Action<int> callback) => _onRowRemoved = callback;
        public void OnRowSelected(Action<int> callback) => _onRowSelected = callback;
        public void OnRowMoved(Action<int, int> callback) => _onRowMoved = callback;
        public void OnRowRenamed(Action<int> callback) => _onRowRenamed = callback;
        public void OnButtonClicked(Action<int> callback) => _onButtonClicked = callback;

        public void Draw<T>(Rect rect, List<T> list)
        {
            _columns ??= BuildColumnsFromElementType(typeof(T));
            ConsumePendingDirty();

            var evt = Event.current;
            if (evt.type == EventType.MouseDown && !rect.Contains(evt.mousePosition))
                GUIUtility.keyboardControl = 0;

            var boxRect = BoxDrawer.CalcBoxRect(rect);
            if (boxRect.width < 1f || boxRect.height < 1f) return;

            BoxDrawer.DrawBox(boxRect);
            var contentRect = BoxDrawer.CalcContentRect(boxRect);
            GUI.BeginGroup(contentRect);

            var toolbarHeight = ShowToolbar ? ControlsToolbar.ToolbarHeight : 0f;
            if (ShowToolbar) DrawToolbar(new Rect(0f, 0f, contentRect.width, toolbarHeight));
            else _searchText = string.Empty;

            var headerHeight = EditorGUIUtility.singleLineHeight + CellPadding * 2f;
            var bodyAvailH = Mathf.Max(0f, contentRect.height - toolbarHeight - headerHeight);
            var filteredIndices = GetFilteredIndices(list);
            var totalRowsH = filteredIndices.Count * ComputeRowHeight();

            var vScrollW = ControlsToolbar.VerticalScrollbarWidth;
            var probeLayout = BuildLayout(contentRect.width);
            var effectiveBodyH = bodyAvailH - (probeLayout.NeedsHorizontalScroll ? ControlsToolbar.HorizontalScrollbarHeight : 0f);
            var needVScroll = totalRowsH > effectiveBodyH;

            var viewWidth = Mathf.Max(120f, contentRect.width - (needVScroll ? vScrollW : 0f));
            var layout = needVScroll ? BuildLayout(viewWidth) : probeLayout;
            if (!layout.NeedsHorizontalScroll) _scrollPos.x = 0f;

            var headerRect = new Rect(0f, toolbarHeight, viewWidth, headerHeight);
            DrawHeader(headerRect, list, layout);
            if (needVScroll)
                PaintCellFrame(new Rect(viewWidth, toolbarHeight, vScrollW, headerHeight), HeaderCellBackground, GridLineColor);

            DrawRows(new Rect(0f, headerRect.yMax, contentRect.width,
                Mathf.Max(0f, contentRect.height - headerRect.yMax)), list, filteredIndices, layout, viewWidth);

            GUI.EndGroup();
            if (evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition))
            {
                GUIUtility.keyboardControl = 0;
                evt.Use();
            }
        }
    }
}
#endif
