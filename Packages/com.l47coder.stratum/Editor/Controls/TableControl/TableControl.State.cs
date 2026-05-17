using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TableControl
    {
        private const float IndexColumnWidth = 28f;
        private const float RowButtonWidth = 24f;
        private const float CellPadding = 4f;
        private const float GridThickness = 1f;

        private const float RowMoveSmoothTime = 0.1f;
        private const float GapMoveSmoothTime = 0.09f;
        private const float DragRowSmoothTime = 0.065f;

        private const float RowIndexDragSlopPixels = 6f;
        private const double RowIndexHoldToDragSeconds = 0.35;

        private static TableControl _rowIndexPendingOwner;

        private static readonly int RowReorderControlHintHash = "TableListAttribute.Row".GetHashCode();

        private static ReorderSession _reorder;
        private static TableControl _draggingOwner;
        private static bool _dragTickHooked;
        private static double _lastUpdateTime;

        private static GUIStyle _headerCellLabelStyleCache;
        private static GUIStyle _bodyIndexLabelStyleCache;
        private static GUIStyle _rowIconButtonStyleCache;
        private static GUIStyle _rowTextButtonStyleCache;
        private static GUIStyle _rowButtonHeaderStyleCache;

        private static bool _rowIndexUpdateHooked;

        private bool _rowIndexInteractDown;
        private int _rowIndexInteractControlId;
        private int _rowIndexInteractDataIndex;
        private Vector2 _rowIndexInteractPressPos;
        private double _rowIndexInteractPressTime;
        private Rect _rowIndexInteractRowRect;
        private float _rowIndexInteractRowHeight;
        private int _rowIndexInteractListCount;
        private bool _rowIndexInteractSearchingAtPress;

        private List<ColumnDefinition> _columns;
        private Type _elementType;
        private object _lastItemsRef;
        private float[] _columnPreferredWidths;
        private float[] _columnMinWidths;
        private int _resizeColumnIndex = -1;
        private float _resizeLastMouseX;
        private int _selectedIndex = -1;
        private Vector2 _scrollPos;

        private Action<int> _onRowEdit;
        private Action<int> _onRowSelect;
        private Action<int> _onRowAdd;
        private Action<int> _onRowRemove;
        private Action<int, int> _onRowMove;
        private Action<int> _onButtonClick;
        private Action<int, int> _onRowButtonClick;
        private Action<int> _onRowDragOut;
        private Action<int> _onRowReceiveDrop;

        private Vector2 _lastGroupSize;
        private int _dropHighlightDataIndex = -1;

        private struct TableLayout
        {
            public float TotalWidth;
            public float IndexWidth;
            public float ActionsWidth;
            public float DataColumnsWidth;
            public float[] DataColumnWidths;
            public bool NeedsHorizontalScroll;
        }

        private sealed class ReorderSession
        {
            public int ArraySize;
            public int ControlId;
            public int SourceIndex;
            public int InsertSlot;
            public float PickupOffsetY;
            public float SourceRowHeight;
            public float[] RowCurrentY;
            public float[] RowTargetY;
            public float[] RowVelocityY;
            public float GapCurrentY;
            public float GapTargetY;
            public float GapVelocityY;
            public float DragRowY;
            public float DragRowYTarget;
            public float DragRowVelY;
            public float LastBodyTopY;
            public bool HasBodyTopY;
            public bool PositionsInitialized;
        }

        private sealed class VisualRow
        {
            public int RowIndex;
            public int StripeIndex;
            public float DrawY;
            public float Height;
            public bool IsGap;
        }

        private static GUIStyle HeaderCellLabelStyle =>
            _headerCellLabelStyleCache ??= new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };

        private static GUIStyle BodyIndexLabelStyle =>
            _bodyIndexLabelStyleCache ??= new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };

        private static GUIStyle RowIconButtonStyle =>
            _rowIconButtonStyleCache ??= new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageOnly,
                fixedWidth = 0f,
                fixedHeight = 0f,
            };

        private static GUIStyle RowTextButtonStyle =>
            _rowTextButtonStyleCache ??= new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.MiddleCenter,
                fixedWidth = 0f,
                fixedHeight = 0f,
            };

        private static GUIStyle RowButtonHeaderStyle =>
            _rowButtonHeaderStyleCache ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageOnly,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };

        private static Color HeaderCellBackground =>
            EditorGUIUtility.isProSkin
                ? new Color(0.24f, 0.26f, 0.29f, 1f)
                : new Color(0.86f, 0.88f, 0.93f, 1f);

        private static Color BodyCellBackground(bool alt) =>
            EditorGUIUtility.isProSkin
                ? (alt ? new Color(0.15f, 0.15f, 0.16f, 1f) : new Color(0.13f, 0.13f, 0.14f, 1f))
                : (alt ? new Color(0.96f, 0.97f, 0.98f, 1f) : new Color(1f, 1f, 1f, 1f));

        private static Color GridLineColor =>
            EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.24f, 1f)
                : new Color(0.62f, 0.64f, 0.68f, 1f);

        private static Color SelectedCellBackground =>
            EditorGUIUtility.isProSkin
                ? new Color(0.26f, 0.46f, 0.70f, 1f)
                : new Color(0.50f, 0.68f, 0.96f, 1f);

        private static Color DropTargetCellBackground =>
            EditorGUIUtility.isProSkin
                ? new Color(0.20f, 0.40f, 0.62f, 0.55f)
                : new Color(0.24f, 0.49f, 0.91f, 0.30f);

        private bool SelectRowCore(int index)
        {
            if (index < 0) return false;
            _selectedIndex = index;
            _onRowSelect?.Invoke(index);
            return true;
        }

        private static Type DetectElementType(IList items)
        {
            if (items == null) return null;
            var t = items.GetType();
            if (t.IsArray) return t.GetElementType();
            if (t.IsGenericType)
            {
                var args = t.GetGenericArguments();
                if (args.Length > 0) return args[0];
            }
            foreach (var iface in t.GetInterfaces())
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IList<>))
                    return iface.GetGenericArguments()[0];
            return typeof(object);
        }

        private static void RequestGuiVisualRefresh() => (EditorWindow.mouseOverWindow ?? EditorWindow.focusedWindow)?.Repaint();

        private void DrawCore(Rect rect)
        {
            var list = Items;
            if (list == null) return;

            if (!ReferenceEquals(list, _lastItemsRef))
            {
                _lastItemsRef = list;
                _elementType = DetectElementType(list);
                _columns = BuildColumnsFromElementType(_elementType);
                _columnMinWidths = null;
                _columnPreferredWidths = null;
            }

            var evt = Event.current;
            if (evt.type == EventType.MouseDown && !rect.Contains(evt.mousePosition))
                GUIUtility.keyboardControl = 0;

            var boxRect = BoxDrawer.CalcBoxRect(rect);
            if (boxRect.width < 1f || boxRect.height < 1f) return;

            BoxDrawer.DrawBox(boxRect);
            var contentRect = BoxDrawer.CalcContentRect(boxRect);
            GUI.BeginGroup(contentRect);
            _lastGroupSize = new Vector2(contentRect.width, contentRect.height);

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
