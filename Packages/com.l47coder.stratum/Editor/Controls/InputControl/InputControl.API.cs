using System;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class InputControl
    {
        public string Value { get; set; } = string.Empty;
        public float FontSize { get; set; } = 12f;

        public void OnChange(Action<string> callback) => _onChange = callback;

        public void Draw(Rect rect) => DrawCore(rect);
    }
}
