using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class ManagerCreatorPage : IPage
    {
        public string GroupTitle => "Manager";
        public string TabTitle   => "Creator";

        private const float IncludeConfigCardHeight = 42f;

        private static readonly Color ConfigCardOnBg      = new(0.16f, 0.33f, 0.22f);
        private static readonly Color ConfigCardOffBg     = new(0.23f, 0.23f, 0.23f);
        private static readonly Color ConfigCardOnBorder  = new(0.30f, 0.78f, 0.42f);
        private static readonly Color ConfigCardOffBorder = new(0.36f, 0.36f, 0.36f);

        private readonly ManagerCreatorState _state = new();
        private Vector2 _scroll;
        private bool _isInitialized;
        private GUIStyle _configTitleStyle;
        private GUIStyle _configDescStyle;

        public void OnEnter()
        {
            if (!_isInitialized)
            {
                _state.Reset();
                _isInitialized = true;
                return;
            }
            _state.RefreshDerivedState();
        }

        public void OnLeave()
        {
            _scroll = Vector2.zero;
            _state.Reset();
            _isInitialized = false;
        }

        public void OnGUI(Rect rect)
        {
            EditorGUI.DrawRect(rect, CreatorPageDraw.BgColor);

            var content = new Rect(
                rect.x + CreatorPageDraw.HPad,
                rect.y + CreatorPageDraw.VPad,
                rect.width  - CreatorPageDraw.HPad * 2f,
                rect.height - CreatorPageDraw.VPad * 2f);

            GUILayout.BeginArea(content);
            var prevLW = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = CreatorPageDraw.LabelWidth;
            try
            {
                DrawInputSection();
                GUILayout.Space(CreatorPageDraw.SectionSpacing);

                if (_state.HasPreview)
                    DrawPreviewSections();

                GUILayout.FlexibleSpace();
                GUILayout.Space(CreatorPageDraw.SectionSpacing);
                DrawCreateButton();
            }
            finally
            {
                EditorGUIUtility.labelWidth = prevLW;
                GUILayout.EndArea();
            }
        }

        private void DrawInputSection()
        {
            CreatorPageDraw.BeginCard();
            CreatorPageDraw.DrawHeader("Input");

            var newName = CreatorPageDraw.DrawEditableField(
                "Manager name", _state.InputManagerName, _state.GetInputStatus());
            if (newName != _state.InputManagerName)
                _state.SetInputManagerName(newName);

            GUILayout.Space(4f);
            DrawIncludeConfigCard();

            if (!string.IsNullOrEmpty(_state.ErrorMessage))
            {
                GUILayout.Space(8f);
                EditorGUILayout.HelpBox(_state.ErrorMessage, MessageType.Warning);
            }

            CreatorPageDraw.EndCard();
        }

        private void DrawPreviewSections()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            try
            {
                CreatorPageDraw.DrawPreviewCard("Type names",   _state.GetNamePreviewItems());
                GUILayout.Space(CreatorPageDraw.SectionSpacing);
                CreatorPageDraw.DrawPreviewCard("Output paths", _state.GetPathPreviewItems());
                if (_state.IncludeConfig)
                {
                    GUILayout.Space(CreatorPageDraw.SectionSpacing);
                    CreatorPageDraw.DrawPreviewCard("Addressables", _state.GetAddressablePreviewItems());
                }
                GUILayout.Space(CreatorPageDraw.SectionSpacing + 2f);
                CreatorPageDraw.DrawLegendRow();
            }
            finally { EditorGUILayout.EndScrollView(); }
        }

        private void DrawCreateButton()
        {
            var prevBg = GUI.backgroundColor;
            if (_state.IsValid) GUI.backgroundColor = CreatorPageDraw.AccentBlue;

            using (new EditorGUI.DisabledScope(!_state.IsValid))
            {
                if (GUILayout.Button("Create Manager", GUILayout.Height(CreatorPageDraw.CreateButtonHeight)))
                {
                    ManagerCreationService.CreateManager(_state);
                    _state.Reset();
                    _scroll = Vector2.zero;
                    GUI.FocusControl(null);
                }
            }

            GUI.backgroundColor = prevBg;
        }

        private void DrawIncludeConfigCard()
        {
            var rect = GUILayoutUtility.GetRect(0f, IncludeConfigCardHeight, GUILayout.ExpandWidth(true));
            var isOn = _state.IncludeConfig;

            EditorGUI.DrawRect(rect, isOn ? ConfigCardOnBg : ConfigCardOffBg);
            CreatorPageDraw.DrawOutline(rect, isOn ? ConfigCardOnBorder : ConfigCardOffBorder);

            var titleRect  = new Rect(rect.x + 10f, rect.y + 5f,  rect.width - 72f, 18f);
            var descRect   = new Rect(rect.x + 10f, rect.y + 21f, rect.width - 72f, 16f);
            var toggleRect = new Rect(
                rect.xMax - 44f,
                rect.y + (rect.height - EditorGUIUtility.singleLineHeight) * 0.5f,
                34f, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(titleRect, "Include config", ConfigTitleStyle);
            EditorGUI.LabelField(descRect,
                isOn
                    ? "Generates the config, data and manager partials under Generated/, plus the config asset and Addressables entry."
                    : "Generates the main script and the manager partial stub under Generated/, without a config asset.",
                ConfigDescStyle);

            var next = EditorGUI.Toggle(toggleRect, isOn);
            if (next != isOn) _state.SetIncludeConfig(next);

            if (Event.current.type == EventType.MouseDown
                && rect.Contains(Event.current.mousePosition)
                && !toggleRect.Contains(Event.current.mousePosition))
            {
                _state.SetIncludeConfig(!isOn);
                GUI.FocusControl(null);
                Event.current.Use();
            }
        }

        private GUIStyle ConfigTitleStyle => _configTitleStyle ??= new GUIStyle(EditorStyles.boldLabel)
        { alignment = TextAnchor.MiddleLeft };

        private GUIStyle ConfigDescStyle => _configDescStyle ??= new GUIStyle(EditorStyles.miniLabel)
        { alignment = TextAnchor.MiddleLeft, wordWrap = false };
    }
}
