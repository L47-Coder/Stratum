using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Stratum.Editor
{
    public static class ControlsToolbar
    {
        public const float ToolbarHeight = 20f;
        public const float ToolbarSeparatorHeight = 1f;
        public const float ToolbarSectionGap = 4f;
        public const float ToolbarButtonSpacing = 2f;
        public const float SearchFieldHeight = 18f;

        public static float VerticalScrollbarWidth
        {
            get
            {
                var w = GUI.skin?.verticalScrollbar?.fixedWidth ?? 0f;
                return w > 0f ? w : 15f;
            }
        }

        public static float HorizontalScrollbarHeight
        {
            get
            {
                var h = GUI.skin?.horizontalScrollbar?.fixedHeight ?? 0f;
                return h > 0f ? h : 15f;
            }
        }

        public static Color DropIndicatorColor =>
            EditorGUIUtility.isProSkin
                ? new Color(0.35f, 0.65f, 1.00f, 1f)
                : new Color(0.10f, 0.45f, 0.85f, 1f);

        private static GUIStyle _titleStyle;
        private static GUIStyle _buttonStyle;
        private static GUIStyle _searchPlaceholderStyle;
        private static bool _searchPlaceholderProSkin;

        public static GUIStyle TitleStyle =>
            _titleStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };

        public static GUIStyle ButtonStyle =>
            _buttonStyle ??= new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 12,
                padding = new RectOffset(2, 2, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.MiddleCenter,
                fixedWidth = 0,
                fixedHeight = 0,
                contentOffset = new Vector2(0, -1f),
            };

        public static GUIStyle SearchPlaceholderStyle
        {
            get
            {
                if (_searchPlaceholderStyle != null && _searchPlaceholderProSkin == EditorGUIUtility.isProSkin)
                    return _searchPlaceholderStyle;
                _searchPlaceholderProSkin = EditorGUIUtility.isProSkin;
                _searchPlaceholderStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip,
                    fontSize = 9,
                    normal =
                {
                    textColor = _searchPlaceholderProSkin
                        ? new Color(0.62f, 0.64f, 0.68f, 0.95f)
                        : new Color(0.42f, 0.45f, 0.50f, 0.95f),
                },
                };
                return _searchPlaceholderStyle;
            }
        }

        public static void DrawToolbarSeparator(Rect toolbarRect)
        {
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(
                new Rect(toolbarRect.x, toolbarRect.yMax - ToolbarSeparatorHeight,
                    toolbarRect.width, ToolbarSeparatorHeight),
                BoxDrawer.BorderColor);
        }

        public static bool DrawSearchBar(
            Rect rect,
            ref SearchField searchField,
            ref string searchText,
            string placeholder,
            bool enabled = true,
            string disabledHint = null)
        {
            searchField ??= new SearchField();

            var fieldRect = new Rect(
                rect.x,
                rect.y + (rect.height - SearchFieldHeight) * 0.5f,
                rect.width,
                SearchFieldHeight);

            var sStyle = GUI.skin.FindStyle("SearchTextField")
                         ?? GUI.skin.FindStyle("ToolbarSearchTextField");
            var origSize = 0;
            var origAlign = TextAnchor.UpperLeft;
            if (sStyle != null)
            {
                origSize = sStyle.fontSize;
                origAlign = sStyle.alignment;
                sStyle.fontSize = 11;
                sStyle.alignment = TextAnchor.MiddleLeft;
            }

            string newText;
            using (new EditorGUI.DisabledScope(!enabled))
                newText = searchField.OnGUI(fieldRect, enabled ? searchText ?? string.Empty : string.Empty);

            if (sStyle != null) { sStyle.fontSize = origSize; sStyle.alignment = origAlign; }

            if (!enabled) newText = string.Empty;

            if (string.IsNullOrEmpty(newText) && Event.current.type == EventType.Repaint)
            {
                var hint = !enabled && !string.IsNullOrEmpty(disabledHint) ? disabledHint : placeholder;
                if (!string.IsNullOrEmpty(hint))
                    GUI.Label(
                        new Rect(fieldRect.x + 18f, fieldRect.y, fieldRect.width - 36f, fieldRect.height),
                        hint, SearchPlaceholderStyle);
            }

            if (newText == searchText) return false;
            searchText = newText;
            GUI.changed = true;
            return true;
        }

        public static bool MatchesGlob(string name, string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return false;
            return GlobMatch((name ?? string.Empty).ToLowerInvariant(), pattern.ToLowerInvariant(), 0, 0);
        }

        private static bool GlobMatch(string text, string pattern, int ti, int pi)
        {
            while (ti < text.Length && pi < pattern.Length)
            {
                if (pattern[pi] == '?') { ti++; pi++; }
                else if (pattern[pi] == '*')
                {
                    while (pi < pattern.Length && pattern[pi] == '*') pi++;
                    if (pi == pattern.Length) return true;
                    while (ti < text.Length) { if (GlobMatch(text, pattern, ti, pi)) return true; ti++; }
                    return false;
                }
                else if (pattern[pi] == text[ti]) { ti++; pi++; }
                else return false;
            }
            while (pi < pattern.Length && pattern[pi] == '*') pi++;
            return ti == text.Length && pi == pattern.Length;
        }
    }
}
