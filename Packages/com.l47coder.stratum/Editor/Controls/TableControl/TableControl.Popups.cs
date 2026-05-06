using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TableControl
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

        // ── 多选下拉（List<string> + Dropdown 方法）────────────────────────────────

        private void DrawMultiSelectStringListCell(Rect rect, List<string> listValue, int rowIndex, FieldInfo field, string dropdownMethodName)
        {
            var summary = listValue.Count == 0 ? "(none)" : string.Join(", ", listValue);
            if (!GUI.Button(rect, summary, StringListSummaryStyle)) return;
            GUI.FocusControl(null);
            var options = InvokeDropdownMethodFresh(field, dropdownMethodName);
            PopupWindow.Show(rect, new StringMultiSelectPopup(options, listValue,
                () => { _pendingDirty = true; _onRowRenamed?.Invoke(rowIndex); }));
        }

        private static string[] InvokeDropdownMethodFresh(FieldInfo field, string methodName)
        {
            var method = field.DeclaringType?.GetMethod(
                methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) return Array.Empty<string>();
            return method.Invoke(null, null) switch
            {
                string[] arr => arr,
                List<string> lst => lst.ToArray(),
                IEnumerable<string> e => e.ToArray(),
                _ => Array.Empty<string>(),
            };
        }

        private static GUIStyle _stringListSummaryStyle;
        private static GUIStyle StringListSummaryStyle =>
            _stringListSummaryStyle ??= new GUIStyle(EditorStyles.textField)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(4, 4, 0, 0),
            };

        private sealed class StringMultiSelectPopup : PopupWindowContent
        {
            private const float RowH = 22f;
            private const float RowGap = 1f;
            private const float Padding = 8f;
            private const float ScrollbarW = 14f;
            private const float BodyMaxH = 280f;
            private const float WindowW = 260f;
            private const float HeaderH = 20f;
            private const float HeaderToBodyGap = 4f;

            private readonly string[] _options;
            private readonly List<string> _selected;
            private readonly Action _onChanged;
            private Vector2 _scroll;

            public StringMultiSelectPopup(string[] options, List<string> selected, Action onChanged)
            {
                _options = options ?? Array.Empty<string>();
                _selected = selected;
                _onChanged = onChanged;
            }

            public override Vector2 GetWindowSize()
            {
                var bodyH = _options.Length == 0
                    ? RowH
                    : Mathf.Min(_options.Length * (RowH + RowGap), BodyMaxH);
                return new Vector2(WindowW, Padding * 2f + HeaderH + HeaderToBodyGap + bodyH);
            }

            public override void OnGUI(Rect rect)
            {
                var inner = new Rect(rect.x + Padding, rect.y + Padding,
                    rect.width - Padding * 2f, rect.height - Padding * 2f);

                var headerLabel = _selected.Count == 0
                    ? $"Labels  ({_options.Length})"
                    : $"Labels  —  {_selected.Count} selected";
                EditorGUI.LabelField(new Rect(inner.x, inner.y, inner.width, HeaderH),
                    headerLabel, EditorStyles.miniBoldLabel);

                var bodyRect = new Rect(inner.x, inner.y + HeaderH + HeaderToBodyGap,
                    inner.width, inner.height - HeaderH - HeaderToBodyGap);

                if (_options.Length == 0)
                {
                    EditorGUI.LabelField(bodyRect, "No labels defined",
                        EditorStyles.centeredGreyMiniLabel);
                    return;
                }

                var contentH = _options.Length * (RowH + RowGap);
                var needScroll = contentH > bodyRect.height + 0.5f;
                var viewW = needScroll ? bodyRect.width - ScrollbarW : bodyRect.width;

                _scroll = GUI.BeginScrollView(bodyRect, _scroll,
                    new Rect(0f, 0f, viewW, Mathf.Max(contentH, bodyRect.height)));

                for (var i = 0; i < _options.Length; i++)
                {
                    var opt = _options[i];
                    var rowRect = new Rect(0f, i * (RowH + RowGap), viewW, RowH);
                    var isSel = _selected.Contains(opt);
                    EditorGUI.BeginChangeCheck();
                    var next = EditorGUI.ToggleLeft(rowRect, opt, isSel);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (next && !isSel) _selected.Add(opt);
                        else if (!next) _selected.Remove(opt);
                        _onChanged?.Invoke();
                        editorWindow?.Repaint();
                    }
                }

                GUI.EndScrollView();
            }
        }

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
