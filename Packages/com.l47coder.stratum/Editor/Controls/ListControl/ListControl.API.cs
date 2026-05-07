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
        public bool CanEdit { get; set; } = true;

        public bool CanReorder { get; set; } = true;
        public bool CanDragOut { get; set; }
        public bool CanReceiveDrop { get; set; }

        public bool ShowToolbar { get; set; } = true;
        public List<string> ExcludePatterns { get; set; } = new();
        public List<GUIContent> ToolbarButtons { get; set; } = new();

        public void OnRowAdd(Action<int> callback) => _onRowAdd = callback;
        public void OnRowRemove(Action<int> callback) => _onRowRemove = callback;
        public void OnRowSelect(Action<int> callback) => _onRowSelect = callback;
        public void OnRowEdit(Action<int> callback) => _onRowEdit = callback;
        public void OnRowMove(Action<int, int> callback) => _onRowMove = callback;
        public void OnRowDragOut(Action<int> callback) => _onRowDragOut = callback;
        public void OnRowReceiveDrop(Action<int> callback) => _onRowReceiveDrop = callback;
        public void OnButtonClick(Action<int> callback) => _onButtonClick = callback;

        public bool SelectRow(int index) => SelectRowCore(index);

        public void Draw(Rect rect, List<string> items) => DrawCore(rect, items);
    }
}
