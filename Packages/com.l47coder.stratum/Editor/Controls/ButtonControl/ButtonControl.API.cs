using System;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class ButtonControl
    {
        public string Label { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public Color? AccentColor { get; set; }
        public float FontSize { get; set; } = 13f;

        public void OnClick(Action callback) => _onClick = callback;

        public void Draw(Rect rect) => DrawCore(rect);
    }
}
