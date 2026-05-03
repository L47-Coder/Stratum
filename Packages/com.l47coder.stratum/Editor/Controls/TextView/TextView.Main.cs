#if UNITY_EDITOR
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TextView
    {
        private static readonly Regex StripTagsRegex = new(@"<[^>]+>", RegexOptions.Compiled);

        private static Color DefaultTextColor =>
            EditorGUIUtility.isProSkin ? new Color(0.62f, 0.62f, 0.64f) : new Color(0.40f, 0.40f, 0.42f);

        private GUIStyle _style;
        private float _styleBuiltFontSize = -1f;
        private bool _styleBuiltWordWrap;
        private Color _styleBuiltColor;
        private string _cachedText;
        private float _cachedClientWidth;
        private bool _cachedWordWrap;
        private float _cachedFontSize = -1f;
        private GUIContent _labelContent;
        private Vector2 _scrollExtent;
        private Vector2 _scrollPos;

        private GUIStyle GetStyle()
        {
            var color = TextColor ?? DefaultTextColor;
            if (_style != null && Mathf.Approximately(_styleBuiltFontSize, FontSize) && _styleBuiltWordWrap == WordWrap && _styleBuiltColor == color)
                return _style;

            _styleBuiltFontSize = FontSize;
            _styleBuiltWordWrap = WordWrap;
            _styleBuiltColor = color;
            _style = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = WordWrap, font = EditorStyles.standardFont, fontSize = Mathf.Max(1, (int)FontSize) };
            _style.normal.textColor = _style.hover.textColor = _style.active.textColor = _style.focused.textColor = color;
            return _style;
        }

        private void HandleContextMenu(Rect rect, string text)
        {
            var evt = Event.current;
            if (evt.type != EventType.ContextClick || !rect.Contains(evt.mousePosition)) return;
            var plain = StripTagsRegex.Replace(text ?? string.Empty, string.Empty);
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy"), false, () => GUIUtility.systemCopyBuffer = plain);
            menu.ShowAsContext();
            evt.Use();
        }
    }
}
#endif
