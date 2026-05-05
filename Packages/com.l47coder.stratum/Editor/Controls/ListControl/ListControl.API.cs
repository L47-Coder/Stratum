using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class ListControl
    {
        public bool CanAdd { get; set; } = true;
        public bool CanRemove { get; set; } = true;
        public bool CanSelect { get; set; } = true;
        public bool CanDrag { get; set; } = true;
        public bool CanRename { get; set; } = true;
        public bool ShowToolbar { get; set; } = true;
        public bool CanReceiveDrop { get; set; }
        public List<string> ExcludePatterns { get; set; } = new();
        public List<GUIContent> ToolbarButtons { get; set; } = new();

        public void OnRowAdded(Action<int> callback) => _onRowAdded = callback;
        public void OnRowRemoved(Action<int> callback) => _onRowRemoved = callback;
        public void OnRowSelected(Action<int> callback) => _onRowSelected = callback;
        public void OnRowMoved(Action<int, int> callback) => _onRowMoved = callback;
        public void OnRowRenamed(Action<int> callback) => _onRowRenamed = callback;
        public void OnDropOnRow(Action<int> callback) => _onDropOnRow = callback;
        public void OnButtonClicked(Action<int> callback) => _onButtonClicked = callback;

        public bool TrySelectRow(int index) => TrySelectRowCore(index);

        public void Draw(Rect rect, List<string> items) => DrawCore(rect, items);
    }
}
