using System;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class ButtonControl
    {
        private GUIStyle _style;
        private int _styleBuiltFontSize = -1;
        private Action _onClick;

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

        private void DrawCore(Rect rect)
        {
            var boxRect = BoxDrawer.CalcBoxRect(rect);
            if (boxRect.width < 1f || boxRect.height < 1f) return;

            BoxDrawer.DrawBox(boxRect);
            var contentRect = BoxDrawer.CalcContentRect(boxRect);

            var prevBg = GUI.backgroundColor;
            if (Enabled && AccentColor.HasValue) GUI.backgroundColor = AccentColor.Value;

            bool clicked;
            using (new EditorGUI.DisabledScope(!Enabled))
            {
                GUI.BeginGroup(contentRect);
                clicked = GUI.Button(
                    new Rect(0f, 0f, contentRect.width, contentRect.height),
                    Label ?? string.Empty, GetStyle());
                GUI.EndGroup();
            }

            GUI.backgroundColor = prevBg;
            if (clicked) _onClick?.Invoke();
        }
    }
}
