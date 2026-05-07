using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal struct SplitterHandle
    {
        public float X;
        private bool _dragging;

        private const float VisualW = 1f;
        private const float HitExtra = 2f;
        private static readonly Color LineColor = new(0.11f, 0.11f, 0.11f);

        public SplitterHandle(float startX) { X = startX; _dragging = false; }

        public (Rect left, Rect right) Draw(Rect rect, float minX, float maxX)
        {
            var visualRect = new Rect(rect.x + X, rect.y, VisualW, rect.height);
            var hitRect = new Rect(rect.x + X - HitExtra, rect.y, VisualW + HitExtra * 2f, rect.height);

            EditorGUIUtility.AddCursorRect(hitRect, MouseCursor.ResizeHorizontal);

            var evt = Event.current;
            switch (evt.type)
            {
                case EventType.MouseDown when hitRect.Contains(evt.mousePosition):
                    _dragging = true; evt.Use(); break;
                case EventType.MouseDrag when _dragging:
                    X = Mathf.Clamp(evt.mousePosition.x - rect.x, minX, Mathf.Min(maxX, rect.width - minX));
                    evt.Use(); break;
                case EventType.MouseUp when _dragging:
                    _dragging = false; evt.Use(); break;
            }

            EditorGUI.DrawRect(visualRect, LineColor);
            return (
                new Rect(rect.x, rect.y, X, rect.height),
                new Rect(visualRect.xMax, rect.y, rect.width - X - VisualW, rect.height)
            );
        }
    }
}
