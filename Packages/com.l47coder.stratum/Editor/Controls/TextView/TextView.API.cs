using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TextView
    {
        public float FontSize { get; set; } = 13f;
        public bool WordWrap { get; set; }
        public Color? TextColor { get; set; }

        public void Draw(Rect rect, string text)
        {
            var boxRect = BoxDrawer.CalcBoxRect(rect);
            if (boxRect.width < 1f || boxRect.height < 1f) return;

            BoxDrawer.DrawBox(boxRect);
            var contentRect = BoxDrawer.CalcContentRect(boxRect);
            GUI.BeginGroup(contentRect);

            var style = GetStyle();
            var fullWidth = contentRect.width;
            var clientWidth = fullWidth - ControlsToolbar.VerticalScrollbarWidth;

            if (_cachedText != text || !Mathf.Approximately(_cachedClientWidth, fullWidth) || _cachedWordWrap != WordWrap || !Mathf.Approximately(_cachedFontSize, FontSize))
            {
                if (_cachedText != text) _scrollPos = Vector2.zero;
                _cachedText = text;
                _cachedClientWidth = fullWidth;
                _cachedWordWrap = WordWrap;
                _cachedFontSize = FontSize;
                _labelContent = new GUIContent(text ?? string.Empty);
                if (WordWrap)
                {
                    var tentativeH = style.CalcHeight(_labelContent, fullWidth);
                    var needVScroll = tentativeH > contentRect.height;
                    var useW = needVScroll ? clientWidth : fullWidth;
                    _scrollExtent = new Vector2(useW, needVScroll ? style.CalcHeight(_labelContent, useW) : tentativeH);
                }
                else
                {
                    var w = Mathf.Max(clientWidth, style.CalcSize(_labelContent).x);
                    _scrollExtent = new Vector2(w, style.CalcHeight(_labelContent, w));
                }
            }

            var viewRect = new Rect(0f, 0f, _scrollExtent.x, _scrollExtent.y);
            _scrollPos = GUI.BeginScrollView(new Rect(0f, 0f, contentRect.width, contentRect.height), _scrollPos, viewRect);
            GUI.Label(viewRect, _labelContent, style);
            GUI.EndScrollView();

            GUI.EndGroup();
            HandleContextMenu(rect, text);
        }
    }
}
