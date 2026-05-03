#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public static class BoxDrawer
    {
        public const float Padding = 4f;
        public const float BorderWidth = 1.5f;

        private static Color _borderColor;
        private static Color _fillColor;
        private static bool _colorsInit;
        private static bool _colorsProSkin;

        public static Color BorderColor
        {
            get { EnsureColors(); return _borderColor; }
        }

        public static Color FillColor
        {
            get { EnsureColors(); return _fillColor; }
        }

        private static void EnsureColors()
        {
            if (_colorsInit && _colorsProSkin == EditorGUIUtility.isProSkin) return;
            _colorsInit = true;
            _colorsProSkin = EditorGUIUtility.isProSkin;
            _borderColor = _colorsProSkin
                ? new Color(0.10f, 0.10f, 0.11f, 1f)
                : new Color(0.60f, 0.60f, 0.62f, 1f);
            _fillColor = _colorsProSkin
                ? new Color(0.18f, 0.18f, 0.19f, 1f)
                : new Color(0.85f, 0.85f, 0.86f, 1f);
        }

        public static Rect CalcBoxRect(Rect outer)
        {
            return new Rect(
                Mathf.Round(outer.x + Padding), Mathf.Round(outer.y + Padding),
                Mathf.Round(outer.width - Padding * 2f), Mathf.Round(outer.height - Padding * 2f));
        }

        public static Rect CalcContentRect(Rect box)
        {
            var bw = BorderWidth;
            return new Rect(box.x + bw, box.y + bw, box.width - bw * 2f, box.height - bw * 2f);
        }

        public static void DrawBox(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;
            EnsureColors();

            var x = Mathf.Round(rect.x);
            var y = Mathf.Round(rect.y);
            var w = Mathf.Round(rect.width);
            var h = Mathf.Round(rect.height);

            EditorGUI.DrawRect(new Rect(x, y, w, h), _fillColor);

            var bw = BorderWidth;
            EditorGUI.DrawRect(new Rect(x, y, w, bw), _borderColor);
            EditorGUI.DrawRect(new Rect(x, y + h - bw, w, bw), _borderColor);
            EditorGUI.DrawRect(new Rect(x, y, bw, h), _borderColor);
            EditorGUI.DrawRect(new Rect(x + w - bw, y, bw, h), _borderColor);
        }
    }
}
#endif
