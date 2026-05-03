#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TableView
    {
        private bool _pendingDirty;

        private void ConsumePendingDirty() { if (_pendingDirty) { _pendingDirty = false; GUI.changed = true; } }

        private static bool IsStringList(Type type) =>
            type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(List<>) &&
            type.GetGenericArguments()[0] == typeof(string);

        private void DrawStringListCell(Rect rect, List<string> listValue, int rowIndex)
        {
            var summary = listValue.Count == 0 ? "(empty)" : $"[{listValue.Count}] {string.Join(", ", listValue)}";
            if (!GUI.Button(rect, summary, StringListSummaryStyle)) return;
            GUI.FocusControl(null);
            PopupWindow.Show(rect, new StringListPopup(listValue, () => { _pendingDirty = true; _onRowRenamed?.Invoke(rowIndex); }));
        }

        private static GUIStyle _stringListSummaryStyle;
        private static GUIStyle StringListSummaryStyle =>
            _stringListSummaryStyle ??= new GUIStyle(EditorStyles.textField)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(4, 4, 0, 0),
            };

        private sealed class StringListPopup : PopupWindowContent
        {
            private const float HeaderH = 20f;
            private const float RowH = 20f;
            private const float RowGap = 2f;
            private const float HeaderToBodyGap = 4f;
            private const float Padding = 6f;
            private const float BtnW = 22f;
            private const float ScrollbarW = 14f;
            private const float BodyMaxH = 280f;
            private const float WindowW = 300f;

            private readonly List<string> _list;
            private readonly Action _onChanged;
            private Vector2 _scroll;

            public StringListPopup(List<string> list, Action onChanged) { _list = list; _onChanged = onChanged; }

            public override Vector2 GetWindowSize()
            {
                var rows = _list.Count;
                var bodyH = rows == 0 ? 0f : Mathf.Min(rows * RowH + (rows - 1) * RowGap, BodyMaxH) + HeaderToBodyGap;
                return new Vector2(WindowW, Padding * 2f + HeaderH + bodyH);
            }

            public override void OnGUI(Rect rect)
            {
                var inner = new Rect(rect.x + Padding, rect.y + Padding, rect.width - Padding * 2f, rect.height - Padding * 2f);
                var bodyAvailH = inner.height - HeaderH - HeaderToBodyGap;
                var contentH = _list.Count * RowH + Mathf.Max(0, _list.Count - 1) * RowGap;
                var needScroll = _list.Count > 0 && contentH > bodyAvailH + 0.5f;
                var rightInset = needScroll ? ScrollbarW : 0f;

                DrawHeader(new Rect(inner.x, inner.y, inner.width - rightInset, HeaderH));
                if (_list.Count == 0) return;
                DrawBody(new Rect(inner.x, inner.y + HeaderH + HeaderToBodyGap, inner.width,
                    Mathf.Max(0f, inner.height - HeaderH - HeaderToBodyGap)), needScroll);
            }

            private void DrawHeader(Rect headerRect)
            {
                var addRect = new Rect(headerRect.xMax - BtnW, headerRect.y, BtnW, headerRect.height);
                EditorGUI.LabelField(new Rect(headerRect.x, headerRect.y, headerRect.width - BtnW - 4f, headerRect.height),
                    $"Strings ({_list.Count})", EditorStyles.miniBoldLabel);
                if (GUI.Button(addRect, "＋"))
                {
                    _list.Add(string.Empty);
                    _onChanged?.Invoke();
                    GUI.FocusControl(null);
                    editorWindow.Repaint();
                }
            }

            private void DrawBody(Rect bodyRect, bool needScroll)
            {
                var viewW = needScroll ? bodyRect.width - ScrollbarW : bodyRect.width;
                var contentH = _list.Count * RowH + Mathf.Max(0, _list.Count - 1) * RowGap;
                _scroll = GUI.BeginScrollView(bodyRect, _scroll, new Rect(0f, 0f, viewW, Mathf.Max(contentH, bodyRect.height)));

                var removeIdx = -1;
                for (var i = 0; i < _list.Count; i++)
                {
                    var rowY = i * (RowH + RowGap);
                    EditorGUI.BeginChangeCheck();
                    var newVal = EditorGUI.TextField(new Rect(0f, rowY, viewW - BtnW - 4f, RowH), _list[i] ?? string.Empty);
                    if (EditorGUI.EndChangeCheck()) { _list[i] = newVal; _onChanged?.Invoke(); }
                    if (GUI.Button(new Rect(viewW - BtnW, rowY, BtnW, RowH), "−")) removeIdx = i;
                }

                if (removeIdx >= 0) { _list.RemoveAt(removeIdx); _onChanged?.Invoke(); GUI.FocusControl(null); editorWindow.Repaint(); }
                GUI.EndScrollView();
            }
        }
    }
}
#endif
