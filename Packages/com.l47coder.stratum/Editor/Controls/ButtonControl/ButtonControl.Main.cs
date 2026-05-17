using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class ButtonControl
    {
        private GUIStyle _style;
        private int _styleBuiltFontSize = -1;

        private GUIStyle GetStyle()
        {
            var size = Mathf.Max(1, (int)FontSize);
            if (_style != null && _styleBuiltFontSize == size) return _style;
            _styleBuiltFontSize = size;
            _style = new GUIStyle(GUI.skin.button)
            {
                fontSize = size,
                alignment = TextAnchor.MiddleCenter,
            };
            return _style;
        }

        private bool DrawCore(Rect rect, string label, bool enabled)
        {
            var boxRect = BoxDrawer.CalcBoxRect(rect);
            if (boxRect.width < 1f || boxRect.height < 1f) return false;

            BoxDrawer.DrawBox(boxRect);
            var contentRect = BoxDrawer.CalcContentRect(boxRect);

            var prevBg = GUI.backgroundColor;
            if (enabled && AccentColor.HasValue) GUI.backgroundColor = AccentColor.Value;

            bool clicked;
            using (new EditorGUI.DisabledScope(!enabled))
            {
                GUI.BeginGroup(contentRect);
                clicked = GUI.Button(
                    new Rect(0f, 0f, contentRect.width, contentRect.height),
                    label ?? string.Empty, GetStyle());
                GUI.EndGroup();
            }

            GUI.backgroundColor = prevBg;
            return clicked;
        }
    }
}
