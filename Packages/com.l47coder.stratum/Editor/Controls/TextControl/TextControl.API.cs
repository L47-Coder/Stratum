using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TextControl
    {
        public float FontSize { get; set; } = 13f;
        public bool WordWrap { get; set; }
        public Color? TextColor { get; set; }

        public void Draw(Rect rect, string text) => DrawCore(rect, text);
    }
}
