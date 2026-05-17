using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class ButtonControl
    {
        public Color? AccentColor { get; set; }
        public float FontSize { get; set; } = 13f;

        public bool Draw(Rect rect, string label, bool enabled = true) => DrawCore(rect, label, enabled);
    }
}
