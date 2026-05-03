using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class FrameworkSyncPage : IPage
    {
        public string GroupTitle => "Framework";
        public string TabTitle => "Sync";

        private const float HPad = 16f;
        private const float VPad = 16f;
        private const float SectionSpacing = 10f;
        private const float CardPad = 14f;
        private const float SyncButtonHeight = 44f;
        private const float RadioRowHeight = 58f;
        private const float RadioRowSpacing = 8f;
        private const float ToggleBoxSize = 18f;

        private static readonly Color BgColor = new(0.17f, 0.17f, 0.17f);
        private static readonly Color CardBg = new(0.215f, 0.215f, 0.215f);
        private static readonly Color CardBorder = new(0.13f, 0.13f, 0.13f);
        private static readonly Color AccentBlue = new(0.35f, 0.65f, 1f);
        private static readonly Color HeaderTextColor = new(0.78f, 0.84f, 0.94f);
        private static readonly Color DimTextColor = new(0.65f, 0.65f, 0.65f);

        private static readonly Color RowBg = new(0.23f, 0.23f, 0.23f);
        private static readonly Color RowBorder = new(0.36f, 0.36f, 0.36f);
        private static readonly Color RowCheckedBg = new(0.20f, 0.35f, 0.50f);
        private static readonly Color RowCheckedBorder = new(0.35f, 0.65f, 1f);

        public void OnGUI(Rect rect)
        {
            EditorGUI.DrawRect(rect, BgColor);

            var content = new Rect(rect.x + HPad, rect.y + VPad, rect.width - HPad * 2f, rect.height - VPad * 2f);

            GUILayout.BeginArea(content);
            try
            {
                DrawIntroCard();
                GUILayout.Space(SectionSpacing);
                DrawSyncButton();
                GUILayout.Space(SectionSpacing);
                DrawTriggerCard();
                GUILayout.FlexibleSpace();
            }
            finally
            {
                GUILayout.EndArea();
            }
        }

        private void DrawIntroCard()
        {
            BeginCard();
            DrawHeader("Sync Runtime");
            GUILayout.Label(
                "Ensure the framework's scaffolding assets exist and run every [EditorSync] method once. "
                + "Refreshers overwrite each ManagerConfig's _configs list, so this is kept behind an "
                + "explicit trigger instead of running on window open.",
                IntroStyle);
            EndCard();
        }

        private static void DrawSyncButton()
        {
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = AccentBlue;
            if (GUILayout.Button("Sync Runtime", GUILayout.Height(SyncButtonHeight)))
                FrameworkSyncSettings.RunSync("manual");
            GUI.backgroundColor = prevBg;
        }

        private void DrawTriggerCard()
        {
            BeginCard();
            DrawHeader("Auto trigger");
            GUILayout.Label(
                "Choose when the sync should also run automatically. The button above is always available regardless of this choice.",
                IntroStyle);
            GUILayout.Space(10f);

            var current = FrameworkSyncSettings.Trigger;
            current = DrawRadioRow(current, FrameworkSyncTrigger.Manual,
                "Manual only",
                "Only run when the Sync Runtime button above is clicked.");
            GUILayout.Space(RadioRowSpacing);
            current = DrawRadioRow(current, FrameworkSyncTrigger.OnWorkbenchClose,
                "When Dev Workbench closes",
                "Run once each time this window is closed.");
            GUILayout.Space(RadioRowSpacing);
            current = DrawRadioRow(current, FrameworkSyncTrigger.BeforePlayMode,
                "Before entering Play Mode",
                "Run once right before the editor enters Play Mode.");

            if (current != FrameworkSyncSettings.Trigger)
                FrameworkSyncSettings.Trigger = current;

            EndCard();
        }

        private static FrameworkSyncTrigger DrawRadioRow(FrameworkSyncTrigger current, FrameworkSyncTrigger value, string label, string desc)
        {
            var selected = current == value;
            var rect = GUILayoutUtility.GetRect(0f, RadioRowHeight, GUILayout.ExpandWidth(true));

            var bg = selected ? RowCheckedBg : RowBg;
            var border = selected ? RowCheckedBorder : RowBorder;
            EditorGUI.DrawRect(rect, bg);
            DrawOutline(rect, border);

            var toggleRect = new Rect(rect.x + 14f, rect.y + (rect.height - ToggleBoxSize) * 0.5f, ToggleBoxSize, ToggleBoxSize);
            var textLeft = toggleRect.xMax + 12f;
            var titleRect = new Rect(textLeft, rect.y + 8f, rect.width - (textLeft - rect.x) - 14f, 20f);
            var descRect = new Rect(textLeft, rect.y + 28f, rect.width - (textLeft - rect.x) - 14f, rect.height - 32f);

            var nextSelected = EditorGUI.Toggle(toggleRect, selected, EditorStyles.radioButton);
            if (nextSelected && !selected) current = value;

            EditorGUI.LabelField(titleRect, label, RowTitleStyle);
            EditorGUI.LabelField(descRect, desc, RowDescStyle);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && !toggleRect.Contains(Event.current.mousePosition))
            {
                current = value;
                GUI.FocusControl(null);
                Event.current.Use();
            }

            return current;
        }

        private static void BeginCard()
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

        private static void EndCard()
        {
            EditorGUILayout.EndVertical();
            GUILayout.Space(CardPad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(CardPad);
            EditorGUILayout.EndVertical();
        }

        private static void DrawHeader(string title)
        {
            GUILayout.Label(title, HeaderStyle);
            GUILayout.Space(3f);
            var line = GUILayoutUtility.GetRect(0f, 2f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(line, new Color(0.30f, 0.30f, 0.30f));
            GUILayout.Space(10f);
        }

        private static void DrawOutline(Rect r, Color c)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1f), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, r.width, 1f), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 1f, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - 1f, r.y, 1f, r.height), c);
        }

        private static GUIStyle _headerStyle;
        private static GUIStyle HeaderStyle => _headerStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            normal = { textColor = HeaderTextColor },
        };

        private static GUIStyle _introStyle;
        private static GUIStyle IntroStyle => _introStyle ??= new GUIStyle(EditorStyles.label)
        {
            fontSize = 12,
            wordWrap = true,
            normal = { textColor = DimTextColor },
        };

        private static GUIStyle _rowTitleStyle;
        private static GUIStyle RowTitleStyle => _rowTitleStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.92f, 0.92f, 0.92f) },
        };

        private static GUIStyle _rowDescStyle;
        private static GUIStyle RowDescStyle => _rowDescStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 11,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true,
            normal = { textColor = DimTextColor },
        };
    }
}
