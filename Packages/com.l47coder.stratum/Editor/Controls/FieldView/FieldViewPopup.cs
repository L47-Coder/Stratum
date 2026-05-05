using System;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    /// <summary>
    /// 字段展示控件（泡泡形式）。与 TableView / ListView 用法一致：new 出来，调 Draw。
    /// 点击外部自动关闭，无标题栏。
    /// </summary>
    public sealed class FieldViewPopup
    {
        private const float PopupW    = 360f;
        private const float PaddingV  = 4f;
        private const float MaxHeight = 420f;

        public bool Readonly { get; set; }

        /// <summary>在 <paramref name="anchorRect"/> 旁弹出字段编辑泡泡。非每帧调用，在按钮点击时调用一次。</summary>
        /// <param name="anchorRect">触发按钮的 GUI 矩形，泡泡从此处弹出。</param>
        /// <param name="item">要展示/编辑的对象（引用类型，字段修改直接作用于原实例）。</param>
        /// <param name="onChanged">字段值变化后的通知回调（用于标脏/保存等），可为 null。</param>
        public void Show<T>(Rect anchorRect, T item, Action onChanged = null)
        {
            PopupWindow.Show(anchorRect, new Content<T>(item, Readonly, onChanged));
        }

        // ── 内部 PopupWindowContent ──────────────────────────────────────────

        private sealed class Content<T> : PopupWindowContent
        {
            private readonly FieldView _view;
            private readonly Action    _onChanged;
            private readonly float     _contentH;

            internal Content(T item, bool @readonly, Action onChanged)
            {
                _view      = new FieldView { Readonly = @readonly };
                _onChanged = onChanged;
                var count  = _view.PrepareForItem(item);
                _contentH  = count > 0 ? _view.CalcContentHeight() : 22f;
            }

            public override Vector2 GetWindowSize() =>
                new(PopupW, Mathf.Min(_contentH + PaddingV * 2f, MaxHeight));

            public override void OnGUI(Rect rect)
            {
                var inner = new Rect(rect.x, rect.y + PaddingV,
                                     rect.width, rect.height - PaddingV * 2f);
                EditorGUI.BeginChangeCheck();
                _view.DrawRows(inner);
                if (EditorGUI.EndChangeCheck())
                    _onChanged?.Invoke();
            }
        }
    }
}
