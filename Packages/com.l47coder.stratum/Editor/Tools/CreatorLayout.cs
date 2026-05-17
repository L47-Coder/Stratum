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
        private readonly string _buttonLabel;
        private readonly Action<TState> _onCreate;

        private readonly InputControl _input = new();
        private readonly TextControl _preview = new();
        private readonly ButtonControl _button;

        public CreatorLayout(TState state, string buttonLabel, Action<TState> onCreate)
        {
            _state = state;
            _buttonLabel = buttonLabel;
            _onCreate = onCreate;
            _button = new ButtonControl { AccentColor = AccentBlue };
        }

        public void OnGUI(Rect rect)
        {
            EditorGUI.DrawRect(rect, BgColor);

            var inputRect = new Rect(rect.x, rect.y, rect.width, InputHeight);
            var btnRect = new Rect(rect.x, rect.yMax - ButtonHeight, rect.width, ButtonHeight);
            var midTop = inputRect.yMax + Spacing;
            var midBottom = btnRect.y - Spacing;
            var midRect = new Rect(rect.x, midTop, rect.width, Mathf.Max(0f, midBottom - midTop));

            var newName = _input.Draw(inputRect, _state.InputClassName);
            if (newName != _state.InputClassName) _state.SetInputClassName(newName);

            if (midRect.height > 8f)
            {
                if (!string.IsNullOrEmpty(_state.ErrorMessage))
                {
                    _preview.TextColor = ErrorColor;
                    _preview.Draw(midRect, _state.ErrorMessage);
                }
                else
                {
                    _preview.TextColor = null;
                    _preview.Draw(midRect, _state.GetPreviewSource());
                }
            }

            if (_button.Draw(btnRect, _buttonLabel, _state.IsValid))
            {
                _onCreate(_state);
                _state.Reset();
                GUI.FocusControl(null);
            }
        }
    }
}
