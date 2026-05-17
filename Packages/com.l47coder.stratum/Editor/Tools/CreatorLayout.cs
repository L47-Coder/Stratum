using System;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal interface ICreatorState
    {
        string InputClassName { get; }
        string ErrorMessage { get; }
        bool IsValid { get; }
        void SetInputClassName(string className);
        string GetPreviewSource();
        void Reset();
    }

    internal sealed class CreatorLayout<TState> where TState : ICreatorState
    {
        private const float InputHeight = 32f;
        private const float ButtonHeight = 40f;
        private const float Spacing = 0f;

        private static readonly Color BgColor = new(0.17f, 0.17f, 0.17f);
        private static readonly Color AccentBlue = new(0.35f, 0.65f, 1f);
        private static readonly Color ErrorColor = new(0.95f, 0.45f, 0.40f);

        private readonly TState _state;

        private readonly InputControl _input = new();
        private readonly TextControl _preview = new();
        private readonly ButtonControl _button = new() { AccentColor = AccentBlue };

        public CreatorLayout(TState state, string buttonLabel, Action<TState> onCreate)
        {
            _state = state;
            _button.Label = buttonLabel;

            _input.OnChange(name => _state.SetInputClassName(name));
            _button.OnClick(() =>
            {
                onCreate(_state);
                _state.Reset();
                GUI.FocusControl(null);
            });
        }

        public void OnGUI(Rect rect)
        {
            EditorGUI.DrawRect(rect, BgColor);

            var inputRect = new Rect(rect.x, rect.y, rect.width, InputHeight);
            var btnRect = new Rect(rect.x, rect.yMax - ButtonHeight, rect.width, ButtonHeight);
            var midTop = inputRect.yMax + Spacing;
            var midBottom = btnRect.y - Spacing;
            var midRect = new Rect(rect.x, midTop, rect.width, Mathf.Max(0f, midBottom - midTop));

            _input.Value = _state.InputClassName;
            _button.Enabled = _state.IsValid;

            if (!string.IsNullOrEmpty(_state.ErrorMessage))
            {
                _preview.TextColor = ErrorColor;
                _preview.Text = _state.ErrorMessage;
            }
            else
            {
                _preview.TextColor = null;
                _preview.Text = _state.GetPreviewSource();
            }

            _input.Draw(inputRect);
            if (midRect.height > 8f) _preview.Draw(midRect);
            _button.Draw(btnRect);
        }
    }
}
