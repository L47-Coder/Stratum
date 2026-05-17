using System;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class InputControl
    {
        private GUIStyle _style;
        private int _styleBuiltFontSize = -1;
        private Action<string> _onChange;

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

        private void DrawCore(Rect rect)
        {
            var boxRect = BoxDrawer.CalcBoxRect(rect);
            if (boxRect.width < 1f || boxRect.height < 1f) return;

            BoxDrawer.DrawBox(boxRect);
            var contentRect = BoxDrawer.CalcContentRect(boxRect);

            GUI.BeginGroup(contentRect);
            var newValue = EditorGUI.TextField(
                new Rect(0f, 0f, contentRect.width, contentRect.height),
                Value ?? string.Empty, GetStyle());
            GUI.EndGroup();

            if (newValue == Value) return;
            Value = newValue;
            _onChange?.Invoke(newValue);
        }
    }
}
