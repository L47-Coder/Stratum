using System;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class FieldPopup
    {
        public bool Readonly { get; set; }
        public void OnClosed(Action callback) => _onChanged = callback;

        public void Show<T>(Rect anchorRect, T item) => ShowCore(anchorRect, item);
    }
}
