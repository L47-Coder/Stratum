using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    /// <summary>
    /// 将单个对象的所有可见字段以"标签 | 控件"竖排方式绘制。
    /// 字段可见性与渲染方式由 <see cref="FieldAttribute"/> 和 <see cref="FieldAttribute.Dropdown"/> 控制。
    /// 仅支持引用类型目标；值类型字段的修改会直接写回对象（盒装修改）。
    /// </summary>
    public sealed partial class FieldView
    {
        /// <summary>全部字段强制只读。</summary>
        public bool Readonly { get; set; }

        /// <summary>
        /// 绘制 <paramref name="item"/> 的所有可见字段。
        /// 内部以 <c>item.GetType()</c>（运行时类型）反射，支持多态引用（如 <c>BaseComponentData</c> 指向子类）。
        /// </summary>
        public void Draw<T>(Rect rect, T item)
        {
            if (item == null)
            {
                GUI.Label(rect, "null", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            var runtimeType = item.GetType();
            if (_cachedType != runtimeType) { _cachedType = runtimeType; _fieldDefs = null; }
            _fieldDefs ??= BuildFieldDefs(runtimeType);

            var boxRect = BoxDrawer.CalcBoxRect(rect);
            if (boxRect.width < 1f || boxRect.height < 1f) return;
            BoxDrawer.DrawBox(boxRect);

            var contentRect = BoxDrawer.CalcContentRect(boxRect);

            var totalH = _fieldDefs.Count * (RowHeight + RowGap) - (_fieldDefs.Count > 0 ? RowGap : 0f);
            var needVScroll = totalH > contentRect.height;
            var viewW = contentRect.width - (needVScroll ? ControlsToolbar.VerticalScrollbarWidth : 0f);
            var viewRect = new Rect(0f, 0f, viewW, Mathf.Max(totalH, contentRect.height));

            GUI.BeginGroup(contentRect);
            _scrollPos = GUI.BeginScrollView(
                new Rect(0f, 0f, contentRect.width, contentRect.height),
                _scrollPos, viewRect, false, needVScroll);

            var boxed = (object)item;
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
