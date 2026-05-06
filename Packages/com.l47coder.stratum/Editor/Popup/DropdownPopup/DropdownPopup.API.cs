using System;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class DropdownPopup
    {
        public bool Multi { get; set; }
        public string Separator { get; set; } = ", ";

        public void OnConfirmed(Action<string> callback) => _onConfirmed = callback;

        public void Show(Rect anchorRect, string[] items, string current) => ShowCore(anchorRect, items, current);
    }
}
