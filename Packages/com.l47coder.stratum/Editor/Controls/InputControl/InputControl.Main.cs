using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class InputControl
    {
        private GUIStyle _style;
        private int _styleBuiltFontSize = -1;

        private GUIStyle GetStyle()
        {
            var size = Mathf.Max(1, (int)FontSize);
            if (_style != null && _styleBuiltFontSize == size) return _style;
            _styleBuiltFontSize = size;
            _style = new GUIStyle(EditorStyles.textField)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = size,
            };
            return _style;
        }

        private string DrawCore(Rect rect, string value)
        {
            var boxRect = BoxDrawer.CalcBoxRect(rect);
            if (boxRect.width < 1f || boxRect.height < 1f) return value;

            BoxDrawer.DrawBox(boxRect);
            var contentRect = BoxDrawer.CalcContentRect(boxRect);

            GUI.BeginGroup(contentRect);
            var result = EditorGUI.TextField(
                new Rect(0f, 0f, contentRect.width, contentRect.height),
                value ?? string.Empty, GetStyle());
            GUI.EndGroup();
            return result;
        }
    }
}
