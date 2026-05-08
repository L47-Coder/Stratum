using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class ComponentCreatorPage : IPage
    {
        public string GroupTitle => "Component";
        public string TabTitle => "Creator";

        private readonly ComponentCreatorState _state = new();
        private Vector2 _scroll;
        private bool _isInitialized;

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
                rect.width - CreatorPageDraw.HPad * 2f,
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
                "Component name", _state.InputComponentName, _state.GetInputStatus());
            if (newName != _state.InputComponentName)
                _state.SetInputComponentName(newName);

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
                CreatorPageDraw.DrawPreviewCard("Type names", _state.GetNamePreviewItems());
                GUILayout.Space(CreatorPageDraw.SectionSpacing);
                CreatorPageDraw.DrawPreviewCard("Output paths", _state.GetPathPreviewItems());
                GUILayout.Space(CreatorPageDraw.SectionSpacing);
                CreatorPageDraw.DrawPreviewCard("Addressables", _state.GetAddressablePreviewItems());
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
                if (GUILayout.Button("Create Component", GUILayout.Height(CreatorPageDraw.CreateButtonHeight)))
                {
                    ComponentCreationService.Execute(_state);
                    _state.Reset();
                    _scroll = Vector2.zero;
                    GUI.FocusControl(null);
                }
            }

            GUI.backgroundColor = prevBg;
        }
    }
}
