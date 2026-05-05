using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class FieldView
    {
        public bool Readonly { get; set; }

        /// <summary>带 BoxDrawer 边框的完整绘制。</summary>
        public void Draw<T>(Rect rect, T item)
        {
            if (item == null)
            {
                GUI.Label(rect, "null", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            _lastItem = item;
            var runtimeType = item.GetType();
            if (_cachedType != runtimeType) { _cachedType = runtimeType; _fieldDefs = null; }
            _fieldDefs ??= BuildFieldDefs(runtimeType);

            var boxRect = BoxDrawer.CalcBoxRect(rect);
            if (boxRect.width < 1f || boxRect.height < 1f) return;
            BoxDrawer.DrawBox(boxRect);

            DrawRows(BoxDrawer.CalcContentRect(boxRect));
        }

        /// <summary>直接在给定区域内绘制行内容，不加 BoxDrawer 边框。适用于 PopupWindowContent 等已有边框的容器。</summary>
        public void DrawContent<T>(Rect rect, T item)
        {
            if (item == null)
            {
                GUI.Label(rect, "null", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            _lastItem = item;
            var runtimeType = item.GetType();
            if (_cachedType != runtimeType) { _cachedType = runtimeType; _fieldDefs = null; }
            _fieldDefs ??= BuildFieldDefs(runtimeType);

            DrawRows(rect);
        }

        private void DrawRows(Rect contentRect)
        {
            var totalH = _fieldDefs.Count * (RowHeight + RowGap) - (_fieldDefs.Count > 0 ? RowGap : 0f);
            var needVScroll = totalH > contentRect.height;
            var viewW = contentRect.width - (needVScroll ? ControlsToolbar.VerticalScrollbarWidth : 0f);
            var viewRect = new Rect(0f, 0f, viewW, Mathf.Max(totalH, contentRect.height));

            GUI.BeginGroup(contentRect);
            _scrollPos = GUI.BeginScrollView(
                new Rect(0f, 0f, contentRect.width, contentRect.height),
                _scrollPos, viewRect, false, needVScroll);

            var boxed = _lastItem;
            var y = 0f;
            for (var i = 0; i < _fieldDefs.Count; i++)
            {
                DrawFieldRow(new Rect(0f, y, viewW, RowHeight), ref boxed, _fieldDefs[i]);
                y += RowHeight + RowGap;
            }

            GUI.EndScrollView();
            GUI.EndGroup();
        }
    }
}
