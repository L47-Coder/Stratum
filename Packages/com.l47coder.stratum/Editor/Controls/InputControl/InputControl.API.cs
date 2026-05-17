using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class InputControl
    {
        public float FontSize { get; set; } = 12f;

        public string Draw(Rect rect, string value) => DrawCore(rect, value);
    }
}
