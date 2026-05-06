using System;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class DropdownPopup
    {
        public static void Show(Rect anchorRect, string[] items, int selectedIndex, Action<int> onSelected)
            => ShowCore(anchorRect, items, selectedIndex, onSelected);
    }
}
