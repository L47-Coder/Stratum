using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal enum PreviewStatus { Neutral, Create, Write, Skip }

    internal readonly struct PreviewItem
    {
        public readonly string Label;
        public readonly string Value;
        public readonly PreviewStatus Status;

        public PreviewItem(string label, string value, PreviewStatus status)
        {
            Label = label;
            Value = value;
            Status = status;
        }
    }

    internal static class CreatorPageDraw
    {
        public const float HPad = 16f;
        public const float VPad = 16f;
        public const float SectionSpacing = 10f;
        public const float CardPad = 14f;
        public const float CreateButtonHeight = 38f;
        public const float LabelWidth = 150f;
        public const float DotSize = 8f;
        public const float DotLeftPad = 4f;
        public const float FieldSpacing = 4f;

        public static readonly Color BgColor = new(0.17f, 0.17f, 0.17f);
        public static readonly Color CardBg = new(0.215f, 0.215f, 0.215f);
        public static readonly Color CardBorder = new(0.13f, 0.13f, 0.13f);
        public static readonly Color AccentBlue = new(0.35f, 0.65f, 1f);
        public static readonly Color HeaderText = new(0.78f, 0.84f, 0.94f);
        public static readonly Color ValueBg = new(0.15f, 0.15f, 0.15f);
        public static readonly Color ValueBorder = new(0.11f, 0.11f, 0.11f);
        public static readonly Color CreateColor = new(0.29f, 0.78f, 0.40f);
        public static readonly Color WriteColor = new(0.95f, 0.75f, 0.22f);
        public static readonly Color SkipColor = new(0.85f, 0.35f, 0.35f);
        public static readonly Color NeutralColor = new(0.50f, 0.50f, 0.50f);

        private static readonly (Color color, string label)[] LegendItems =
        {
            (CreateColor, "Create"),
            (WriteColor,  "Write"),
            (SkipColor,   "Skip"),
        };

        private static GUIStyle _headerStyle;
        public static GUIStyle HeaderStyle => _headerStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            normal = { textColor = HeaderText },
        };

        private static GUIStyle _inputFieldStyle;
        public static GUIStyle InputFieldStyle => _inputFieldStyle ??= new GUIStyle(EditorStyles.textField)
        {
            alignment = TextAnchor.MiddleLeft,
        };

        private static GUIStyle _valueLabelStyle;
        public static GUIStyle ValueLabelStyle => _valueLabelStyle ??= new GUIStyle(EditorStyles.label)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.55f, 0.55f, 0.55f) },
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip,
        };

        public static void BeginCard()
        {
            var rect = EditorGUILayout.BeginVertical();
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, CardBg);
                DrawOutline(rect, CardBorder);
            }
            GUILayout.Space(CardPad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(CardPad);
            EditorGUILayout.BeginVertical();
        }

        public static void EndCard()
        {
            EditorGUILayout.EndVertical();
            GUILayout.Space(CardPad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(CardPad);
            EditorGUILayout.EndVertical();
        }

        public static void DrawHeader(string title)
        {
            GUILayout.Label(title, HeaderStyle);
            GUILayout.Space(3f);
            var line = GUILayoutUtility.GetRect(0f, 2f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(line, new Color(0.30f, 0.30f, 0.30f));
            GUILayout.Space(10f);
        }

        public static void DrawOutline(Rect r, Color c)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1f), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, r.width, 1f), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 1f, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - 1f, r.y, 1f, r.height), c);
        }

        private static Texture2D _circleTex;
        private static Texture2D CircleTex
        {
            get
            {
                if (_circleTex != null) return _circleTex;
                const int sz = 16;
                _circleTex = new Texture2D(sz, sz, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.DontSave,
                };
                var center = new Vector2((sz - 1) * 0.5f, (sz - 1) * 0.5f);
                var radius = sz * 0.5f;
                for (var y = 0; y < sz; y++)
                    for (var x = 0; x < sz; x++)
                    {
                        var dist = Vector2.Distance(new Vector2(x, y), center);
                        _circleTex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - dist)));
                    }
                _circleTex.Apply();
                return _circleTex;
            }
        }

        public static void DrawDot(Rect rect, Color color)
        {
            var prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, CircleTex);
            GUI.color = prev;
        }

        public static void DrawLegendRow()
        {
            var r = EditorGUILayout.GetControlRect(false, 16f);
            var x = r.x + 2f;
            foreach (var (color, label) in LegendItems)
            {
                var dotRect = new Rect(x, r.y + (r.height - DotSize) * 0.5f, DotSize, DotSize);
                DrawDot(dotRect, color);
                x += DotSize + 4f;
                var size = EditorStyles.miniLabel.CalcSize(new GUIContent(label));
                EditorGUI.LabelField(new Rect(x, r.y, size.x, r.height), label, EditorStyles.miniLabel);
                x += size.x + 16f;
            }
        }

        public static Color StatusColor(PreviewStatus s) => s switch
        {
            PreviewStatus.Create => CreateColor,
            PreviewStatus.Write => WriteColor,
            PreviewStatus.Skip => SkipColor,
            _ => NeutralColor,
        };

        public static void DrawFieldLabel(Rect fullRect, string label, PreviewStatus status)
        {
            var dotRect = new Rect(
                fullRect.x + DotLeftPad,
                fullRect.y + (fullRect.height - DotSize) * 0.5f,
                DotSize, DotSize);
            DrawDot(dotRect, StatusColor(status));

            var labelRect = new Rect(
                dotRect.xMax + 6f, fullRect.y,
                EditorGUIUtility.labelWidth - DotSize - DotLeftPad - 6f,
                fullRect.height);
            EditorGUI.LabelField(labelRect, label);
        }

        public static string DrawEditableField(string label, string value, PreviewStatus status)
        {
            var r = EditorGUILayout.GetControlRect(false, 20f);
            DrawFieldLabel(r, label, status);
            var valueRect = new Rect(
                r.x + EditorGUIUtility.labelWidth + 2f, r.y,
                r.width - EditorGUIUtility.labelWidth - 2f, r.height);
            return EditorGUI.TextField(valueRect, value ?? string.Empty, InputFieldStyle);
        }

        public static void DrawReadonlyField(string label, string value, PreviewStatus status)
        {
            var r = EditorGUILayout.GetControlRect(false, 20f);
            DrawFieldLabel(r, label, status);
            var valueRect = new Rect(
                r.x + EditorGUIUtility.labelWidth + 2f, r.y,
                r.width - EditorGUIUtility.labelWidth - 2f, r.height);

            EditorGUI.DrawRect(valueRect, ValueBg);
            DrawOutline(valueRect, ValueBorder);
            var textRect = new Rect(valueRect.x + 6f, valueRect.y, valueRect.width - 12f, valueRect.height);
            EditorGUI.LabelField(textRect, value ?? string.Empty, ValueLabelStyle);
        }

        public static void DrawPreviewCard(string title, params PreviewItem[] items)
        {
            BeginCard();
            DrawHeader(title);
            for (var i = 0; i < items.Length; i++)
            {
                if (i > 0) GUILayout.Space(FieldSpacing);
                DrawReadonlyField(items[i].Label, items[i].Value, items[i].Status);
            }
            EndCard();
        }
    }
}
