using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TextControl
    {
        public string Text { get; set; } = string.Empty;
        public float FontSize { get; set; } = 13f;
        public bool WordWrap { get; set; }
        public Color? TextColor { get; set; }

        public void Draw(Rect rect) => DrawCore(rect);
    }
}
