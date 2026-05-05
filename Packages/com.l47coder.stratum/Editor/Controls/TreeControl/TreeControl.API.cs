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
        public bool CanDrag { get; set; } = true;
        public bool CanRename { get; set; } = true;
        public bool ShowToolbar { get; set; } = true;
        public List<GUIContent> ToolbarButtons { get; set; } = new();
        public List<string> ExcludePatterns { get; set; } = new();
        public List<string> HiddenExtensions { get; set; }

        public void OnNodeAdded(Action<string> callback) => _onNodeAdded = callback;
        public void OnNodeRemoved(Action<string> callback) => _onNodeRemoved = callback;
        public void OnNodeSelected(Action<string> callback) => _onNodeSelected = callback;
        public void OnNodeMoved(Action<string, string> callback) => _onNodeMoved = callback;
        public void OnNodeRenamed(Action<string, string> callback) => _onNodeRenamed = callback;
        public void OnButtonClicked(Action<int> callback) => _onButtonClicked = callback;

        public void Draw(Rect rect, string path) => DrawCore(rect, path);
    }
}
