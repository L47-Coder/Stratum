using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TableControl
    {
        public bool CanAdd { get; set; } = true;
        public bool CanRemove { get; set; } = true;
        public bool CanSelect { get; set; } = true;
        public bool CanDrag { get; set; } = true;
        public bool CanRename { get; set; } = true;
        public bool ShowToolbar { get; set; } = true;
        public bool MarkDuplicates { get; set; } = true;
        public string KeyField { get; set; } = "Key";
        public List<GUIContent> ToolbarButtons { get; set; } = new();

        public void OnRowAdded(Action<int> callback) => _onRowAdded = callback;
        public void OnRowRemoved(Action<int> callback) => _onRowRemoved = callback;
        public void OnRowSelected(Action<int> callback) => _onRowSelected = callback;
        public void OnRowMoved(Action<int, int> callback) => _onRowMoved = callback;
        public void OnRowRenamed(Action<int> callback) => _onRowRenamed = callback;
        public void OnButtonClicked(Action<int> callback) => _onButtonClicked = callback;

        public void OnExpandFieldAt(Action<int, string, Rect> callback) => _onExpandFieldAt = callback;

        public void Draw<T>(Rect rect, List<T> list) => DrawCore(rect, list);
    }
}
