using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TableView
    {
        private const float IndexColumnWidth = 28f;
        private const float RowButtonWidth = 24f;
        private const float CellPadding = 4f;
        private const float GridThickness = 1f;
        private const float DefaultFallbackMinWidth = 80f;

        private const float RowMoveSmoothTime = 0.1f;
        private const float GapMoveSmoothTime = 0.09f;
        private const float DragRowSmoothTime = 0.065f;

        private const float RowIndexDragSlopPixels = 6f;
        private const double RowIndexHoldToDragSeconds = 0.35;

        private static TableView _rowIndexPendingOwner;

        private static readonly int RowReorderControlHintHash = "TableListAttribute.Row".GetHashCode();

        private static ReorderSession _reorder;
        private static TableView _draggingOwner;
        private static bool _dragTickHooked;
        private static double _lastUpdateTime;

        private static GUIStyle _headerCellLabelStyleCache;
        private static GUIStyle _bodyIndexLabelStyleCache;

        private static readonly Dictionary<FieldInfo, string[]> _dropdownOptionsCache = new();

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
        private float[] _columnPreferredWidths;
        private float[] _columnMinWidths;
        private int _resizeColumnIndex = -1;
        private float _resizeLastMouseX;
        private int _selectedIndex = -1;
        private Vector2 _scrollPos;

        private Action<int> _onRowRenamed;
        private Action<int> _onRowSelected;
        private Action<int> _onRowAdded;
        private Action<int> _onRowRemoved;
        private Action<int, int> _onRowMoved;
        private Action<int> _onButtonClicked;

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

        private static void RequestGuiVisualRefresh()
        {
            GUI.changed = true;
            (EditorWindow.mouseOverWindow ?? EditorWindow.focusedWindow)?.Repaint();
        }
    }
}
