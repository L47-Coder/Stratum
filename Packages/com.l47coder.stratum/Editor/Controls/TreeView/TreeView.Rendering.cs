using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TreeView
    {
        private void DrawToolbar(Rect r)
        {
            ControlsToolbar.DrawToolbarSeparator(r);
            var buttons = ToolbarButtons;
            var btnSize = ControlsToolbar.SearchFieldHeight;
            var spacing = ControlsToolbar.ToolbarButtonSpacing;
            var xMax = r.xMax;

            if (buttons != null && buttons.Count > 0)
            {
                var stripW = buttons.Count * btnSize + (buttons.Count - 1) * spacing;
                var x0 = r.xMax - stripW;
                xMax = x0 - ControlsToolbar.ToolbarSectionGap;
                for (var i = 0; i < buttons.Count; i++)
                {
                    var c = buttons[i] ?? new GUIContent($"{i + 1}", "Empty button");
                    var br = new Rect(x0 + i * (btnSize + spacing), r.y + (r.height - btnSize) * 0.5f, btnSize, btnSize);
                    if (GUI.Button(br, c, c.image != null ? EditorStyles.iconButton : ControlsToolbar.ButtonStyle))
                    { GUI.FocusControl(null); _onButtonClicked?.Invoke(i); }
                }
            }

            DrawSearchBar(new Rect(r.x, r.y, Mathf.Max(xMax - r.x, 20f), r.height));
        }

        private static GUIStyle _labelStyle;
        private static GUIStyle _labelStyleBold;
        private static GUIStyle _foldoutStyle;
        private static GUIStyle LabelStyle => _labelStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
        private static GUIStyle LabelStyleBold => _labelStyleBold ??= new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft };
        private static GUIStyle FoldoutStyle => _foldoutStyle ??= new GUIStyle(EditorStyles.foldout);

        private static Color NodeTextColor(NodeKind kind) => kind switch
        {
            NodeKind.Root => EditorGUIUtility.isProSkin ? new Color(1.00f, 1.00f, 1.00f, 1f) : new Color(0.08f, 0.08f, 0.08f, 1f),
            NodeKind.Branch => EditorGUIUtility.isProSkin ? new Color(1.00f, 0.72f, 0.30f, 1f) : new Color(0.70f, 0.36f, 0.00f, 1f),
            NodeKind.FolderLeaf or NodeKind.FileLeaf => EditorGUIUtility.isProSkin ? new Color(0.55f, 0.90f, 0.55f, 1f) : new Color(0.10f, 0.50f, 0.12f, 1f),
            NodeKind.ReadOnlyFile or NodeKind.ReadOnlyFolder => EditorGUIUtility.isProSkin ? new Color(0.65f, 0.65f, 0.65f, 0.50f) : new Color(0.40f, 0.40f, 0.40f, 0.50f),
            _ => EditorGUIUtility.isProSkin ? new Color(0.78f, 0.78f, 0.78f, 1f) : new Color(0.22f, 0.22f, 0.22f, 1f),
        };

        private static Color RowBg(int index, bool selected, bool dropFolder = false)
        {
            if (selected) return EditorGUIUtility.isProSkin ? new Color(0.17f, 0.36f, 0.53f, 1f) : new Color(0.24f, 0.49f, 0.91f, 1f);
            if (dropFolder) return EditorGUIUtility.isProSkin ? new Color(0.20f, 0.40f, 0.62f, 0.55f) : new Color(0.24f, 0.49f, 0.91f, 0.30f);
            return index % 2 == 0
                ? (EditorGUIUtility.isProSkin ? new Color(0.19f, 0.19f, 0.19f, 1f) : new Color(0.84f, 0.84f, 0.84f, 1f))
                : (EditorGUIUtility.isProSkin ? new Color(0.17f, 0.17f, 0.17f, 1f) : new Color(0.78f, 0.78f, 0.78f, 1f));
        }

        private void DrawBody(Rect bodyRect, List<FlatNode> flatList)
        {
            var totalH = flatList.Count * RowHeight;
            var needScroll = totalH > bodyRect.height;
            var innerW = Mathf.Max(bodyRect.width - (needScroll ? ControlsToolbar.VerticalScrollbarWidth : 0f), 1f);

            _scrollPos = GUI.BeginScrollView(bodyRect, _scrollPos, new Rect(0f, 0f, innerW, Mathf.Max(totalH, bodyRect.height)));
            for (var i = 0; i < flatList.Count; i++)
                DrawRow(new Rect(0f, i * RowHeight, innerW, RowHeight), flatList[i], i);

            if (_renamingPath != null && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                var hit = false;
                for (var i = 0; i < flatList.Count; i++)
                {
                    if (!string.Equals(flatList[i].Node.FullPath, _renamingPath, StringComparison.OrdinalIgnoreCase)) continue;
                    var rowY = i * RowHeight;
                    hit = Event.current.mousePosition.y >= rowY && Event.current.mousePosition.y < rowY + RowHeight;
                    break;
                }
                if (!hit) CommitRename(true);
            }

            if (_dropLineRow >= 0 && Event.current.type == EventType.Repaint)
            {
                var c = ControlsToolbar.DropIndicatorColor;
                const float lH = 1.5f, tH = 6f, tW = 1.5f;
                var lX = _dropLineDepth * IndentWidth + ArrowWidth;
                var lY = _dropLineRow * RowHeight - lH * 0.5f;
                var lW = Mathf.Max(0f, innerW - lX - tW);
                EditorGUI.DrawRect(new Rect(lX, lY - (tH - lH) * 0.5f, tW, tH), c);
                EditorGUI.DrawRect(new Rect(lX + tW, lY, lW, lH), c);
            }

            GUI.EndScrollView();
        }

        private void DrawRow(Rect rowRect, FlatNode flat, int rowIndex)
        {
            var node = flat.Node;
            var isSelected = CanSelect && string.Equals(node.FullPath, _selectedPathBacking, StringComparison.OrdinalIgnoreCase);
            var isDropFolder = string.Equals(node.FullPath, _dropFolderPath, StringComparison.OrdinalIgnoreCase);
            var isRenaming = _renamingPath != null && string.Equals(node.FullPath, _renamingPath, StringComparison.OrdinalIgnoreCase);

            if (Event.current.type == EventType.Repaint) EditorGUI.DrawRect(rowRect, RowBg(rowIndex, isSelected, isDropFolder));

            var x = rowRect.x + flat.Depth * IndentWidth;

            var arrowRect = new Rect(x, rowRect.y, ArrowWidth, RowHeight);
            DrawExpandArrow(arrowRect, node);
            x += ArrowWidth;

            DrawNodeIcon(new Rect(x, rowRect.y + (RowHeight - IconSize) * 0.5f, IconSize, IconSize), node);
            x += IconSize + 2f;

            var labelRect = new Rect(x, rowRect.y, Mathf.Max(0f, rowRect.xMax - x - 2f), RowHeight);
            if (isRenaming)
            {
                DrawRenameField(labelRect);
            }
            else
            {
                var isSearching = !string.IsNullOrEmpty(_searchNormalized);
                var name = node.Kind == NodeKind.Root && isSearching ? "Search results" : node.Name;
                if (node.Kind is NodeKind.FileLeaf or NodeKind.ReadOnlyFile) name = StripKnownExtension(name);
                var prev = GUI.contentColor;
                GUI.contentColor = isSelected ? Color.white : NodeTextColor(node.Kind);
                GUI.Label(labelRect, name, node.Kind == NodeKind.Root ? LabelStyleBold : LabelStyle);
                GUI.contentColor = prev;
                HandleRowInput(rowRect, arrowRect, flat);
            }
        }

        private static void DrawExpandArrow(Rect arrowRect, TreeNode node)
        {
            if (node.Children == null || node.Children.Count == 0) return;
            var expanded = EditorGUI.Foldout(arrowRect, node.IsExpanded, GUIContent.none, true, FoldoutStyle);
            if (expanded == node.IsExpanded) return;
            node.IsExpanded = expanded;
            GUI.changed = true;
        }

        private static void DrawNodeIcon(Rect rect, TreeNode node)
        {
            if (Event.current.type != EventType.Repaint) return;
            var icon = node.Kind == NodeKind.Root
                ? EditorGUIUtility.IconContent("FolderOpened Icon").image
                : AssetDatabase.GetCachedIcon(node.FullPath)
                  ?? EditorGUIUtility.IconContent(node.Kind is NodeKind.Branch or NodeKind.ReadOnlyFolder ? "Folder Icon" : "DefaultAsset Icon").image;
            if (icon != null) GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
        }

        private void DrawRenameField(Rect rect)
        {
            var ctrl = RenameCtrl;
            var wantText = _renameBuffer ?? string.Empty;
            var hasFocus = GUI.GetNameOfFocusedControl() == ctrl;
            GUI.SetNextControlName(ctrl);

            if (_renameFocusRequest)
            {
                EditorGUI.FocusTextInControl(ctrl);
                EditorGUI.TextField(rect, wantText);
                if (hasFocus)
                {
                    if (GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) is TextEditor ed)
                    { ed.text = wantText; ed.SelectAll(); }
                    _renameFocusRequest = false;
                }
            }
            else _renameBuffer = EditorGUI.TextField(rect, wantText);

            if (hasFocus) _renameHadFocus = true;
        }
    }
}
