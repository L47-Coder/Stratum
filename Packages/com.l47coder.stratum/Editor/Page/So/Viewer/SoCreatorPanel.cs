using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class SoCreatorPanel
    {
        private const float HPad = 6f;
        private const float VPad = 8f;

        private readonly SoCreatorState _state = new();
        private Vector2 _scroll;

        public void Retarget(string parentFolderAssetPath) =>
            _state.SetParentFolder(parentFolderAssetPath);

        public void OnGUI(Rect rect)
        {
            EditorGUI.DrawRect(rect, CreatorPageDraw.BgColor);

            var content = new Rect(
                rect.x + HPad, rect.y + VPad,
                rect.width - HPad * 2f, rect.height - VPad * 2f);

            GUILayout.BeginArea(content);
            var prevLW = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = CreatorPageDraw.LabelWidth;
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            try
            {
                DrawInputSection();

                if (_state.HasPreview)
                {
                    GUILayout.Space(CreatorPageDraw.SectionSpacing);
                    CreatorPageDraw.DrawPreviewCard("Type names",   _state.GetNamePreviewItems());
                    GUILayout.Space(CreatorPageDraw.SectionSpacing);
                    CreatorPageDraw.DrawPreviewCard("Output paths", _state.GetPathPreviewItems());
                    GUILayout.Space(CreatorPageDraw.SectionSpacing + 2f);
                    CreatorPageDraw.DrawLegendRow();
                }

                GUILayout.Space(CreatorPageDraw.SectionSpacing);
                DrawCreateButton();
            }
            finally
            {
                EditorGUILayout.EndScrollView();
                EditorGUIUtility.labelWidth = prevLW;
                GUILayout.EndArea();
            }
        }

        private void DrawInputSection()
        {
            CreatorPageDraw.BeginCard();
            CreatorPageDraw.DrawHeader("New ScriptableObject type");

            var newName = CreatorPageDraw.DrawEditableField(
                "Type name", _state.InputName, _state.GetInputStatus());
            if (newName != _state.InputName)
                _state.SetInputName(newName);

            if (!string.IsNullOrEmpty(_state.ErrorMessage))
            {
                GUILayout.Space(8f);
                EditorGUILayout.HelpBox(_state.ErrorMessage, MessageType.Warning);
            }

            CreatorPageDraw.EndCard();
        }

        private void DrawCreateButton()
        {
            var prevBg = GUI.backgroundColor;
            if (_state.IsValid) GUI.backgroundColor = CreatorPageDraw.AccentBlue;

            using (new EditorGUI.DisabledScope(!_state.IsValid))
            {
                if (GUILayout.Button("Create SO Type", GUILayout.Height(CreatorPageDraw.CreateButtonHeight)))
                {
                    SoCreationService.CreateSoType(_state);
                    _state.Reset();
                    _scroll = Vector2.zero;
                    GUI.FocusControl(null);
                }
            }

            GUI.backgroundColor = prevBg;
        }
    }
}
