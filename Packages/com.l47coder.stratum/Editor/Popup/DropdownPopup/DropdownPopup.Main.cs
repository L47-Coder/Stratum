using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class DropdownPopup
    {
        private const float RowH = 22f;
        private const float PaddingV = 4f;
        private const float ScrollbarW = 14f;
        private const float MaxH = 280f;
        private const float MinW = 120f;
        private const float SearchGap = 4f;
        private const float SearchPadH = 2f;
        private const float SearchH = 16f;
        private const float CheckmarkW = 22f;
        private const float TextPadL = 6f;

        private Action<string> _onConfirmed;

        private void ShowCore(Rect anchorRect, string[] items, string current)
        {
            if (items == null || items.Length == 0) return;
            PopupWindow.Show(anchorRect, new Content(items, current, Multi, Separator, Search, Mathf.Max(anchorRect.width, MinW), _onConfirmed));
        }

        private sealed class Content : PopupWindowContent
        {
            private readonly string[] _items;
            private readonly bool _multi;
            private readonly string _separator;
            private readonly bool _searchEnabled;
            private readonly float _width;
            private readonly Action<string> _onConfirmed;
            private readonly HashSet<string> _selected;
            private readonly string _initialValue;
            private readonly List<int> _visible = new();

            private string _filter = string.Empty;
            private bool _focusedSearch;
            private Vector2 _scroll;
            private int _hoverVisibleIndex = -1;
            private bool _changed;
            private bool _confirmedSingle;
            private string _confirmedSingleValue;

            internal Content(string[] items, string current, bool multi, string separator, bool searchEnabled, float width, Action<string> onConfirmed)
            {
                _items = items;
                _multi = multi;
                _separator = string.IsNullOrEmpty(separator) ? ", " : separator;
                _searchEnabled = searchEnabled;
                _width = width;
                _onConfirmed = onConfirmed;
                _initialValue = current ?? string.Empty;
                _selected = ParseSelected(current, _separator, multi);
                RebuildVisible();
            }

            private void RebuildVisible()
            {
                _visible.Clear();
                if (string.IsNullOrEmpty(_filter))
                {
                    for (var i = 0; i < _items.Length; i++)
                        _visible.Add(i);
                    return;
                }

                for (var i = 0; i < _items.Length; i++)
                {
                    if (_items[i].IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        _visible.Add(i);
                }
            }

            private float ListAreaHeight
            {
                get
                {
                    if (_visible.Count == 0)
                        return _searchEnabled && !string.IsNullOrEmpty(_filter) ? RowH : 0f;
                    return Mathf.Min(_visible.Count * RowH, MaxH);
                }
            }

            private float SearchExtraHeight => _searchEnabled ? SearchH + SearchGap : 0f;

            public override Vector2 GetWindowSize() =>
                new(_width, PaddingV * 2f + SearchExtraHeight + ListAreaHeight);

            private void ApplyWindowSize()
            {
                if (editorWindow == null) return;
                var sz = GetWindowSize();
                var pos = editorWindow.position;
                pos.width = sz.x;
                pos.height = sz.y;
                editorWindow.position = pos;
            }

            public override void OnGUI(Rect rect)
            {
                var yTop = rect.y + PaddingV;
                var usableW = rect.width;

                if (_searchEnabled)
                {
                    var searchRect = new Rect(rect.x + SearchPadH, yTop, usableW - SearchPadH * 2f, SearchH);
                    GUI.SetNextControlName("StratumDropdownSearch");
                    EditorGUI.BeginChangeCheck();
                    _filter = EditorGUI.TextField(searchRect, _filter, SearchFieldStyle);
                    if (EditorGUI.EndChangeCheck())
                    {
                        RebuildVisible();
                        ApplyWindowSize();
                    }

                    if (!_focusedSearch && Event.current.type == EventType.Layout)
                    {
                        EditorGUI.FocusTextInControl("StratumDropdownSearch");
                        _focusedSearch = true;
                    }

                    yTop += SearchH + SearchGap;
                }

                var inner = new Rect(rect.x, yTop, rect.width, Mathf.Max(0f, rect.yMax - PaddingV - yTop));
                var contentH = _visible.Count == 0 && _searchEnabled && !string.IsNullOrEmpty(_filter)
                    ? RowH
                    : _visible.Count * RowH;
                var needScroll = contentH > inner.height + 0.5f;
                var viewW = needScroll ? inner.width - ScrollbarW : inner.width;

                _scroll = GUI.BeginScrollView(inner, _scroll,
                    new Rect(0f, 0f, viewW, Mathf.Max(contentH, inner.height)));

                var newHover = -1;
                var e = Event.current;

                if (_visible.Count == 0)
                {
                    if (_searchEnabled && !string.IsNullOrEmpty(_filter))
                        GUI.Label(new Rect(0f, 0f, viewW, RowH), "（无匹配）", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    for (var vi = 0; vi < _visible.Count; vi++)
                    {
                        var itemIndex = _visible[vi];
                        var rowRect = new Rect(0f, vi * RowH, viewW, RowH);
                        if (rowRect.Contains(e.mousePosition)) newHover = vi;
                        DrawRow(rowRect, itemIndex, vi);
                    }
                }

                if (e.type == EventType.MouseMove && newHover != _hoverVisibleIndex)
                {
                    _hoverVisibleIndex = newHover;
                    editorWindow?.Repaint();
                }

                GUI.EndScrollView();
            }

            public override void OnClose()
            {
                if (_onConfirmed == null) return;
                if (_multi)
                {
                    if (!_changed) return;
                    var ordered = _items.Where(_selected.Contains).ToArray();
                    _onConfirmed.Invoke(string.Join(_separator, ordered));
                    return;
                }
                if (_confirmedSingle && _confirmedSingleValue != _initialValue)
                    _onConfirmed.Invoke(_confirmedSingleValue);
            }

            private void DrawRow(Rect rowRect, int itemIndex, int visibleIndex)
            {
                var item = _items[itemIndex];
                var isSelected = _multi ? _selected.Contains(item) : item == _initialValue;
                var isHover = visibleIndex == _hoverVisibleIndex;
                var e = Event.current;

                if (e.type == EventType.Repaint)
                {
                    if (isSelected) EditorGUI.DrawRect(rowRect, SelectedBg);
                    else if (isHover) EditorGUI.DrawRect(rowRect, HoverBg);

                    var textRect = new Rect(rowRect.x + TextPadL, rowRect.y, rowRect.width - TextPadL - CheckmarkW, rowRect.height);
                    GUI.Label(textRect, item, isSelected ? SelectedRowStyle : NormalRowStyle);

                    if (isSelected)
                        GUI.Label(new Rect(rowRect.xMax - CheckmarkW, rowRect.y, CheckmarkW, rowRect.height), "✓", CheckmarkStyle);
                }

                EditorGUIUtility.AddCursorRect(rowRect, MouseCursor.Link);

                if (e.type == EventType.MouseDown && e.button == 0 && rowRect.Contains(e.mousePosition))
                {
                    GUI.FocusControl(null);
                    e.Use();
                    if (_multi)
                    {
                        if (!_selected.Add(item)) _selected.Remove(item);
                        _changed = true;
                        editorWindow?.Repaint();
                    }
                    else
                    {
                        _confirmedSingle = true;
                        _confirmedSingleValue = item;
                        editorWindow?.Close();
                    }
                }
            }

            private static HashSet<string> ParseSelected(string current, string separator, bool multi) =>
                string.IsNullOrEmpty(current) || !multi
                    ? new HashSet<string>(StringComparer.Ordinal)
                    : new HashSet<string>(
                        current.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .Where(s => s.Length > 0),
                        StringComparer.Ordinal);
        }

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
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0),
            };

        private static GUIStyle _selectedRowStyle;
        private static GUIStyle SelectedRowStyle =>
            _selectedRowStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(0, 0, 0, 0),
                normal = { textColor = Color.white },
            };

        private static GUIStyle _searchFieldStyle;

        private static GUIStyle SearchFieldStyle =>
            _searchFieldStyle ??= new GUIStyle(EditorStyles.toolbarSearchField)
            {
                fixedHeight = 0f,
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
            };

        private static GUIStyle _checkmarkStyle;
        private static GUIStyle CheckmarkStyle =>
            _checkmarkStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
            };
    }
}
