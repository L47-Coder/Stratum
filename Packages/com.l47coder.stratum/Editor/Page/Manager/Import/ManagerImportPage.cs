using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class ManagerImportPage : IPage
    {
        private const string TemplateRoot = WorkbenchPaths.TemplatesRoot + "/Managers";
        private const float HPad = 12f;
        private const float VPad = 12f;
        private const float RowHeight = 34f;
        private const float RowSpacing = 4f;
        private const float ImportButtonHeight = 44f;
        private const float ToggleBoxSize = 16f;
        private const float FooterGap = 8f;

        private static readonly Color BgColor = new(0.17f, 0.17f, 0.17f);
        private static readonly Color RowBg = new(0.23f, 0.23f, 0.23f);
        private static readonly Color RowInstalledBg = new(0.16f, 0.30f, 0.20f);
        private static readonly Color RowCheckedBg = new(0.20f, 0.34f, 0.48f);
        private static readonly Color RowBorder = new(0.35f, 0.35f, 0.35f);
        private static readonly Color RowCheckedBorder = new(0.35f, 0.65f, 1f);
        private static readonly Color AccentBlue = new(0.35f, 0.65f, 1f);
        private static readonly Color TextColor = new(0.92f, 0.92f, 0.92f);
        private static readonly Color DimTextColor = new(0.65f, 0.65f, 0.65f);

        public string GroupTitle => "Manager";
        public string TabTitle => "Import";

        private readonly HashSet<string> _selected = new(StringComparer.Ordinal);
        private readonly ButtonControl _importButton = new() { AccentColor = AccentBlue };
        private IReadOnlyList<TemplateEntry> _templates;
        private Dictionary<string, bool> _installedState;
        private Vector2 _scroll;

        public ManagerImportPage()
        {
            _importButton.OnClick(ImportSelected);
        }

        public void OnEnter() => RefreshState();
        public void OnLeave() => _scroll = Vector2.zero;

        public void OnGUI(Rect rect)
        {
            EditorGUI.DrawRect(rect, BgColor);
            if (_templates == null) RefreshState();

            var content = new Rect(rect.x + HPad, rect.y + VPad, rect.width - HPad * 2f, rect.height - VPad * 2f);
            var buttonRect = new Rect(content.x, content.yMax - ImportButtonHeight, content.width, ImportButtonHeight);
            var listRect = new Rect(content.x, content.y, content.width, Mathf.Max(0f, content.height - ImportButtonHeight - FooterGap));

            DrawTemplateArea(listRect);
            DrawImportButton(buttonRect);
        }

        private void RefreshState()
        {
            _templates = LoadTemplates();
            _installedState = _templates.ToDictionary(t => t.Name, t => IsTemplateInstalled(t), StringComparer.Ordinal);
            _selected.RemoveWhere(name => !_installedState.ContainsKey(name) || _installedState[name]);
        }

        private void DrawTemplateArea(Rect rect)
        {
            if (_templates.Count == 0)
            {
                GUI.Label(rect, "No Manager templates found.", EmptyStyle);
                return;
            }

            DrawTemplateList(rect);
        }

        private bool IsInstalled(string name) =>
            _installedState != null && _installedState.TryGetValue(name, out var installed) && installed;

        private void DrawTemplateList(Rect rect)
        {
            var contentHeight = Mathf.Max(0f, _templates.Count * (RowHeight + RowSpacing) - RowSpacing);
            var needsScroll = contentHeight > rect.height;
            var scrollbarWidth = needsScroll ? GUI.skin.verticalScrollbar.fixedWidth : 0f;
            if (scrollbarWidth <= 0f && needsScroll) scrollbarWidth = 16f;
            var viewRect = new Rect(0f, 0f, Mathf.Max(0f, rect.width - scrollbarWidth), contentHeight);

            _scroll = GUI.BeginScrollView(rect, _scroll, viewRect);
            for (var i = 0; i < _templates.Count; i++)
            {
                var rowRect = new Rect(0f, i * (RowHeight + RowSpacing), viewRect.width, RowHeight);
                DrawTemplateRow(rowRect, _templates[i]);
            }
            GUI.EndScrollView();
        }

        private void DrawTemplateRow(Rect rect, TemplateEntry template)
        {
            var installed = IsInstalled(template.Name);
            var checkedState = installed || _selected.Contains(template.Name);

            var bg = installed ? RowInstalledBg : checkedState ? RowCheckedBg : RowBg;
            var border = checkedState && !installed ? RowCheckedBorder : RowBorder;
            EditorGUI.DrawRect(rect, bg);
            DrawOutline(rect, border);

            var toggleRect = new Rect(rect.x + 10f, rect.y + (rect.height - ToggleBoxSize) * 0.5f, ToggleBoxSize, ToggleBoxSize);
            var labelLeft = toggleRect.xMax + 10f;
            var labelPad = labelLeft - rect.x;
            var labelRect = new Rect(labelLeft, rect.y, Mathf.Max(0f, rect.width - labelPad * 2f), rect.height);

            using (new EditorGUI.DisabledScope(installed))
            {
                var next = EditorGUI.Toggle(toggleRect, checkedState);
                if (!installed && next != checkedState)
                    SetSelected(template.Name, next);
            }

            GUI.Label(labelRect, template.Name, RowLabelStyle);

            if (!installed && Event.current.type == EventType.MouseDown
                && rect.Contains(Event.current.mousePosition)
                && !toggleRect.Contains(Event.current.mousePosition))
            {
                SetSelected(template.Name, !_selected.Contains(template.Name));
                GUI.FocusControl(null);
                Event.current.Use();
            }
        }

        private void SetSelected(string name, bool selected)
        {
            if (selected) _selected.Add(name);
            else _selected.Remove(name);
        }

        private void DrawImportButton(Rect rect)
        {
            _importButton.Enabled = _selected.Count > 0;
            _importButton.Label = _selected.Count > 0
                ? $"Import {_selected.Count}"
                : "Import";
            _importButton.Draw(rect);
        }

        private void ImportSelected()
        {
            var selected = _templates.Where(t => _selected.Contains(t.Name)).ToArray();
            if (selected.Length == 0) return;

            var imported = 0;
            foreach (var template in selected)
            {
                if (IsTemplateInstalled(template)) continue;
                if (AssetTransporter.Transfer(template.SourceAssetPath, WorkbenchPaths.ManagerRoot) > 0)
                    imported++;
            }

            _selected.Clear();
            ManagerAssetIndex.Invalidate();
            ManagerOrderSync.SyncAsset();
            RefreshState();

            if (imported > 0)
                Debug.Log($"[ManagerImportPage] Imported {imported} Manager template(s).");
        }

        private static IReadOnlyList<TemplateEntry> LoadTemplates()
        {
            var rootAbs = ResolveAssetPathAbsolute(TemplateRoot);
            if (string.IsNullOrEmpty(rootAbs) || !Directory.Exists(rootAbs))
                return Array.Empty<TemplateEntry>();

            try
            {
                return Directory.EnumerateFiles(rootAbs, "*.cs", SearchOption.TopDirectoryOnly)
                    .Select(path => new TemplateEntry(
                        Path.GetFileNameWithoutExtension(path),
                        $"{TemplateRoot}/{Path.GetFileName(path)}"))
                    .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ManagerImportPage] Failed to scan Manager templates: {ex.Message}");
                return Array.Empty<TemplateEntry>();
            }
        }

        private static bool IsTemplateInstalled(TemplateEntry template)
        {
            var rootAbs = ResolveAssetPathAbsolute(WorkbenchPaths.ManagerRoot);
            if (string.IsNullOrEmpty(rootAbs) || !Directory.Exists(rootAbs)) return false;

            try
            {
                return Directory.EnumerateFiles(rootAbs, $"{template.Name}.cs", SearchOption.AllDirectories).Any();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ManagerImportPage] Failed to scan installed Managers: {ex.Message}");
                return false;
            }
        }

        private static string ResolveAssetPathAbsolute(string assetPath)
        {
            try { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath)); }
            catch { return null; }
        }

        private static void DrawOutline(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private readonly struct TemplateEntry
        {
            public readonly string Name;
            public readonly string SourceAssetPath;

            public TemplateEntry(string name, string sourceAssetPath)
            {
                Name = name;
                SourceAssetPath = sourceAssetPath;
            }
        }

        private static GUIStyle _rowLabelStyle;
        private static GUIStyle RowLabelStyle => _rowLabelStyle ??= new GUIStyle(EditorStyles.label)
        { alignment = TextAnchor.MiddleLeft, normal = { textColor = TextColor } };

        private static GUIStyle _emptyStyle;
        private static GUIStyle EmptyStyle => _emptyStyle ??= new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        { normal = { textColor = DimTextColor } };
    }
}
