#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TableView
    {
        private TableLayout BuildLayout(float viewWidth)
        {
            var indexWidth = IndexColumnWidth + CellPadding * 2f;
            var actionsWidth = (CanAdd || CanRemove) ? RowButtonWidth + CellPadding * 2f : 0f;
            var fixedTotal = indexWidth + actionsWidth;
            var availableForData = Mathf.Max(10f, viewWidth - fixedTotal);

            EnsureColumnSizing();

            var count = _columns.Count;
            var widths = new float[count];

            float dataColumnsWidth;
            bool needsHorizontalScroll;
            if (count == 0)
            {
                dataColumnsWidth = availableForData;
                needsHorizontalScroll = false;
            }
            else
            {
                var lastIdx = count - 1;
                var sumOthers = 0f;
                for (var i = 0; i < lastIdx; i++)
                {
                    widths[i] = Mathf.Max(_columnPreferredWidths[i], _columnMinWidths[i]);
                    sumOthers += widths[i];
                }

                var minLast = _columnMinWidths[lastIdx];
                var baseMin = sumOthers + minLast;
                var useOverflowLayout = _resizeColumnIndex >= 0 || baseMin > availableForData + 0.5f;

                if (useOverflowLayout)
                {
                    var baseTotal = 0f;
                    for (var i = 0; i < count; i++)
                    {
                        widths[i] = Mathf.Max(_columnPreferredWidths[i], _columnMinWidths[i]);
                        baseTotal += widths[i];
                    }

                    dataColumnsWidth = baseTotal;
                    needsHorizontalScroll = baseTotal > availableForData + 0.5f;
                }
                else
                {
                    widths[lastIdx] = Mathf.Max(minLast, availableForData - sumOthers);
                    dataColumnsWidth = availableForData;
                    needsHorizontalScroll = false;
                }
            }

            return new TableLayout
            {
                TotalWidth = fixedTotal + dataColumnsWidth,
                IndexWidth = indexWidth,
                ActionsWidth = actionsWidth,
                DataColumnsWidth = dataColumnsWidth,
                DataColumnWidths = widths,
                NeedsHorizontalScroll = needsHorizontalScroll,
            };
        }

        private static float ComputeRowHeight() => EditorGUIUtility.singleLineHeight + CellPadding * 2f;

        private static void PaintCellFrame(Rect outer, Color fill, Color grid)
        {
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(outer, fill);
            DrawRectOutline(outer, grid, GridThickness);
        }

        private static void DrawRectOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static Rect PaddedRect(Rect outer) => new(
            outer.x + CellPadding,
            outer.y + CellPadding,
            Mathf.Max(0f, outer.width - CellPadding * 2f),
            Mathf.Max(0f, outer.height - CellPadding * 2f));
    }
}
#endif
