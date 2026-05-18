using System;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class InputControl
    {
        public float FontSize { get; set; } = 12f;

        public void OnChange(Action<string> callback) => _onChange = callback;

        public string GetValue() => _value;
        public void SetValue(string value) => _value = value ?? string.Empty;
        public void Draw(Rect rect) => DrawCore(rect);
    }
}
