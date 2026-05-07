using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TreeControl
    {
        public bool CanAdd { get; set; } = true;
        public bool CanRemove { get; set; } = true;
        public bool CanSelect { get; set; } = true;
        public bool CanEdit { get; set; } = true;

        public bool CanReorder { get; set; } = true;
        public bool CanDragOut { get; set; } = false;
        public bool CanReceiveDrop { get; set; } = true;

        public bool ShowToolbar { get; set; } = true;
        public List<string> ExcludePatterns { get; set; } = new();
        public List<string> HiddenExtensions { get; set; }
        public List<GUIContent> ToolbarButtons { get; set; } = new();

        public void OnNodeAdd(Action<string> callback) => _onNodeAdd = callback;
        public void OnNodeRemove(Action<string> callback) => _onNodeRemove = callback;
        public void OnNodeSelect(Action<string> callback) => _onNodeSelect = callback;
        public void OnNodeEdit(Action<string, string> callback) => _onNodeEdit = callback;
        public void OnNodeMove(Action<string, string> callback) => _onNodeMove = callback;
        public void OnNodeDragOut(Action<string> callback) => _onNodeDragOut = callback;
        public void OnNodeReceiveDrop(Action<string, string> callback) => _onNodeReceiveDrop = callback;
        public void OnButtonClick(Action<int> callback) => _onButtonClick = callback;

        public bool SelectNode(string path) => SelectNodeCore(path);
        public void RebuildTree() => _root = null;

        public void Draw(Rect rect, string path) => DrawCore(rect, path);
    }
}
