using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed partial class FieldView
    {
        internal bool Readonly { get; set; }

        /// <summary>
        /// 在 contentRect 内直接绘制字段行，不附加任何边框。
        /// 调用前须确保已通过 PrepareForItem 刷新字段定义。
        /// </summary>
        internal void DrawRows(Rect contentRect)
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

        /// <summary>刷新字段定义缓存并存储当前对象，返回可见字段数量。</summary>
        internal int PrepareForItem<T>(T item)
        {
            if (item == null) { _lastItem = null; return 0; }
            _lastItem = item;
            var runtimeType = item.GetType();
            if (_cachedType != runtimeType) { _cachedType = runtimeType; _fieldDefs = null; }
            _fieldDefs ??= BuildFieldDefs(runtimeType);
            return _fieldDefs.Count;
        }

        /// <summary>已准备好数据后计算总内容高度（不含滚动条）。</summary>
        internal float CalcContentHeight() =>
            _fieldDefs == null || _fieldDefs.Count == 0
                ? 0f
                : _fieldDefs.Count * (RowHeight + RowGap) - RowGap;
    }
}
