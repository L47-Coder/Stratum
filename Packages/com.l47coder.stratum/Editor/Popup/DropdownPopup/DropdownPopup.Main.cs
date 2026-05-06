using System;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class DropdownPopup
    {
        private const float RowH       = 22f;
        private const float PaddingV   = 4f;
        private const float ScrollbarW = 14f;
        private const float MaxH       = 280f;
        private const float MinW       = 120f;
        private const float CheckmarkW = 22f;
        private const float TextPadL   = 6f;

        private static void ShowCore(Rect anchorRect, string[] items, int selectedIndex, Action<int> onSelected)
        {
            if (items == null || items.Length == 0) return;
            PopupWindow.Show(anchorRect,
                new Content(items, selectedIndex, onSelected, Mathf.Max(anchorRect.width, MinW)));
        }

        // ─────────────────────────────────────────────────────────────────────────

        private sealed class Content : PopupWindowContent
        {
            private readonly string[]    _items;
            private readonly int         _selectedIndex;
            private readonly Action<int> _onSelected;
            private readonly float       _width;

            private Vector2 _scroll;
            private int     _hoverIndex = -1;

            internal Content(string[] items, int selectedIndex, Action<int> onSelected, float width)
            {
                _items         = items;
                _selectedIndex = selectedIndex;
                _onSelected    = onSelected;
                _width         = width;
            }

            public override Vector2 GetWindowSize() =>
                new(_width, PaddingV * 2f + Mathf.Min(_items.Length * RowH, MaxH));

            public override void OnGUI(Rect rect)
            {
                var inner = new Rect(rect.x, rect.y + PaddingV,
                    rect.width, rect.height - PaddingV * 2f);

                var contentH   = _items.Length * RowH;
                var needScroll = contentH > inner.height + 0.5f;
                var viewW      = needScroll ? inner.width - ScrollbarW : inner.width;

                _scroll = GUI.BeginScrollView(inner, _scroll,
                    new Rect(0f, 0f, viewW, Mathf.Max(contentH, inner.height)));

                var newHover = -1;
                var e        = Event.current;

                for (var i = 0; i < _items.Length; i++)
                {
                    var rowRect = new Rect(0f, i * RowH, viewW, RowH);
                    if (rowRect.Contains(e.mousePosition)) newHover = i;
                    DrawRow(rowRect, i);
                }

                if (e.type == EventType.MouseMove && newHover != _hoverIndex)
                {
                    _hoverIndex = newHover;
                    editorWindow?.Repaint();
                }

                GUI.EndScrollView();
            }

            private void DrawRow(Rect rowRect, int index)
            {
                var isSelected = index == _selectedIndex;
                var isHover    = index == _hoverIndex;
                var e          = Event.current;

                if (e.type == EventType.Repaint)
                {
                    if (isSelected)
                        EditorGUI.DrawRect(rowRect, SelectedBg);
                    else if (isHover)
                        EditorGUI.DrawRect(rowRect, HoverBg);

                    var textRect = new Rect(
                        rowRect.x + TextPadL, rowRect.y,
                        rowRect.width - TextPadL - CheckmarkW, rowRect.height);
                    GUI.Label(textRect, _items[index],
                        isSelected ? SelectedRowStyle : NormalRowStyle);

                    if (isSelected)
                        GUI.Label(
                            new Rect(rowRect.xMax - CheckmarkW, rowRect.y, CheckmarkW, rowRect.height),
                            "✓", CheckmarkStyle);
                }

                EditorGUIUtility.AddCursorRect(rowRect, MouseCursor.Link);

                if (e.type == EventType.MouseDown && e.button == 0
                    && rowRect.Contains(e.mousePosition))
                {
                    GUI.FocusControl(null);
                    e.Use();
                    _onSelected?.Invoke(index);
                    editorWindow?.Close();
                }
            }
        }

        // ─────────────────────────────────────────────────── 颜色 & 样式 ────────

        private static Color SelectedBg =>
            EditorGUIUtility.isProSkin
                ? new Color(0.26f, 0.46f, 0.70f, 1f)
                : new Color(0.50f, 0.68f, 0.96f, 1f);

        private static Color HoverBg =>
            EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.06f)
                : new Color(0f, 0f, 0f, 0.06f);

        private static GUIStyle _normalRowStyle;
        private static GUIStyle NormalRowStyle =>
            _normalRowStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping  = TextClipping.Clip,
                padding   = new RectOffset(0, 0, 0, 0),
            };

        private static GUIStyle _selectedRowStyle;
        private static GUIStyle SelectedRowStyle =>
            _selectedRowStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping  = TextClipping.Clip,
                fontStyle = FontStyle.Bold,
                padding   = new RectOffset(0, 0, 0, 0),
                normal    = { textColor = Color.white },
            };

        private static GUIStyle _checkmarkStyle;
        private static GUIStyle CheckmarkStyle =>
            _checkmarkStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white },
            };
    }
}
