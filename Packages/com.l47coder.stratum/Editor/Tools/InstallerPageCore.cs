using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class InstallerPageCore
    {
        internal readonly struct PackageInfo
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly string Description;
            public readonly bool Recommended;

            public PackageInfo(string id, string displayName, string description, bool recommended)
            {
                Id = id; DisplayName = displayName; Description = description; Recommended = recommended;
            }
        }

        private const float HPad = 16f;
        private const float VPad = 16f;
        private const float SectionSpacing = 10f;
        private const float CardPad = 14f;
        private const float RowHeight = 62f;
        private const float RowSpacing = 8f;
        private const float ImportButtonHeight = 38f;
        private const float ToggleBoxSize = 18f;
        private const float SelectAllRowHeight = 28f;

        private static readonly Color BgColor            = new(0.17f,  0.17f,  0.17f);
        private static readonly Color CardBg             = new(0.215f, 0.215f, 0.215f);
        private static readonly Color CardBorder         = new(0.13f,  0.13f,  0.13f);
        private static readonly Color RowSelectableBg    = new(0.23f,  0.23f,  0.23f);
        private static readonly Color RowInstalledBg     = new(0.16f,  0.33f,  0.22f);
        private static readonly Color RowSelectableBorder= new(0.36f,  0.36f,  0.36f);
        private static readonly Color RowInstalledBorder = new(0.30f,  0.78f,  0.42f);
        private static readonly Color RowCheckedBg       = new(0.20f,  0.35f,  0.50f);
        private static readonly Color RowCheckedBorder   = new(0.35f,  0.65f,  1f);
        private static readonly Color AccentBlue         = new(0.35f,  0.65f,  1f);
        private static readonly Color HeaderTextColor    = new(0.78f,  0.84f,  0.94f);
        private static readonly Color DimTextColor       = new(0.65f,  0.65f,  0.65f);
        private static readonly Color OkTextColor        = new(0.50f,  0.85f,  0.60f);
        private static readonly Color SelectAllBg        = new(0.19f,  0.21f,  0.25f);
        private static readonly Color SelectAllBgActive  = new(0.22f,  0.30f,  0.42f);
        private static readonly Color SelectAllBorder    = new(0.32f,  0.40f,  0.55f);
        private static readonly Color SelectAllBorderActive = new(0.45f, 0.70f, 1f);
        private static readonly Color SelectAllTextColor = new(0.82f,  0.88f,  0.98f);

        public string GroupTitle { get; }
        public string TabTitle   { get; }

        private readonly string _introHeader;
        private readonly string _introBodyText;
        private readonly string _emptyHelpBoxText;
        private readonly string _logTag;
        private readonly Action _invalidateCache;
        private readonly Func<IReadOnlyList<PackageInfo>> _loadPackages;
        private readonly Func<string, bool> _checkIsInstalled;
        private readonly Func<IList<string>, int> _performInstall;

        private readonly HashSet<string> _selected = new();
        private IReadOnlyList<PackageInfo> _packages;
        private Dictionary<string, bool> _installedState;
        private Vector2 _scroll;

        public InstallerPageCore(
            string groupTitle,
            string tabTitle,
            string introHeader,
            string introBodyText,
            string emptyHelpBoxText,
            string logTag,
            Action invalidateCache,
            Func<IReadOnlyList<PackageInfo>> loadPackages,
            Func<string, bool> checkIsInstalled,
            Func<IList<string>, int> performInstall)
        {
            GroupTitle       = groupTitle;
            TabTitle         = tabTitle;
            _introHeader     = introHeader;
            _introBodyText   = introBodyText;
            _emptyHelpBoxText= emptyHelpBoxText;
            _logTag          = logTag;
            _invalidateCache = invalidateCache;
            _loadPackages    = loadPackages;
            _checkIsInstalled= checkIsInstalled;
            _performInstall  = performInstall;
        }

        public void OnEnter() => RefreshState();

        public void OnLeave() => _scroll = Vector2.zero;

        public void OnGUI(Rect rect)
        {
            EditorGUI.DrawRect(rect, BgColor);
            if (_packages == null) RefreshState();

            var content = new Rect(rect.x + HPad, rect.y + VPad, rect.width - HPad * 2f, rect.height - VPad * 2f);
            GUILayout.BeginArea(content);
            try
            {
                DrawIntroCard();
                GUILayout.Space(SectionSpacing);
                if (_packages.Count > 0)
                {
                    DrawSelectAllRow();
                    GUILayout.Space(RowSpacing);
                }
                DrawPackageList();
                GUILayout.FlexibleSpace();
                GUILayout.Space(SectionSpacing);
                DrawImportButton();
            }
            finally { GUILayout.EndArea(); }
        }

        private void RefreshState()
        {
            _invalidateCache();
            _packages = _loadPackages();
            _installedState = _packages.ToDictionary(p => p.Id, p => _checkIsInstalled(p.Id));
            _selected.RemoveWhere(id => !_installedState.ContainsKey(id) || _installedState[id]);
        }

        private void DrawSelectAllRow()
        {
            var installableCount = CountInstallable();
            var allSelected = installableCount > 0 && _selected.Count >= installableCount;
            var disabled = installableCount == 0;

            var rect = GUILayoutUtility.GetRect(0f, SelectAllRowHeight, GUILayout.ExpandWidth(true));
            var bg     = allSelected && !disabled ? SelectAllBgActive : SelectAllBg;
            var border = allSelected && !disabled ? SelectAllBorderActive : SelectAllBorder;
            EditorGUI.DrawRect(rect, bg);
            DrawOutline(rect, border);

            var toggleRect = new Rect(rect.x + 14f, rect.y + (rect.height - ToggleBoxSize) * 0.5f, ToggleBoxSize, ToggleBoxSize);
            var countWidth = 90f;
            var textLeft   = toggleRect.xMax + 12f;
            var labelRect  = new Rect(textLeft, rect.y, rect.width - (textLeft - rect.x) - countWidth - 12f, rect.height);
            var countRect  = new Rect(rect.xMax - countWidth - 12f, rect.y, countWidth, rect.height);

            using (new EditorGUI.DisabledScope(disabled))
            {
                var next = EditorGUI.Toggle(toggleRect, allSelected);
                if (!disabled && next != allSelected) ToggleSelectAll(allSelected);
            }

            var labelText = disabled
                ? "All packages installed — nothing to import"
                : allSelected
                    ? "All installable packages selected (click to clear)"
                    : "Select all installable packages";
            EditorGUI.LabelField(labelRect, labelText, SelectAllLabelStyle);
            EditorGUI.LabelField(countRect, disabled ? "-" : $"{_selected.Count} selected", SelectAllCountStyle);

            if (!disabled && Event.current.type == EventType.MouseDown
                && rect.Contains(Event.current.mousePosition)
                && !toggleRect.Contains(Event.current.mousePosition))
            {
                ToggleSelectAll(allSelected);
                GUI.FocusControl(null);
                Event.current.Use();
            }
        }

        private void ToggleSelectAll(bool currentlyAllSelected)
        {
            if (currentlyAllSelected) _selected.Clear();
            else foreach (var pkg in _packages) { if (!IsInstalled(pkg.Id)) _selected.Add(pkg.Id); }
        }

        private int CountInstallable()
        {
            var n = 0;
            foreach (var pkg in _packages) if (!IsInstalled(pkg.Id)) n++;
            return n;
        }

        private bool IsInstalled(string id) => _installedState != null && _installedState.TryGetValue(id, out var v) && v;

        private void DrawIntroCard()
        {
            BeginCard();
            DrawHeader(_introHeader);
            GUILayout.Label(_introBodyText, IntroStyle);
            if (_packages.Count == 0)
            {
                GUILayout.Space(6f);
                EditorGUILayout.HelpBox(_emptyHelpBoxText, MessageType.Info);
            }
            EndCard();
        }

        private void DrawPackageList()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            try
            {
                foreach (var pkg in _packages)
                {
                    DrawPackageRow(pkg);
                    GUILayout.Space(RowSpacing);
                }
            }
            finally { EditorGUILayout.EndScrollView(); }
        }

        private void DrawPackageRow(PackageInfo pkg)
        {
            var isInstalled = _installedState.TryGetValue(pkg.Id, out var flag) && flag;
            var isChecked   = isInstalled || _selected.Contains(pkg.Id);
            var rect = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));

            Color bg, border;
            if (isInstalled)      { bg = RowInstalledBg;  border = RowInstalledBorder; }
            else if (isChecked)   { bg = RowCheckedBg;    border = RowCheckedBorder; }
            else                  { bg = RowSelectableBg; border = RowSelectableBorder; }

            EditorGUI.DrawRect(rect, bg);
            DrawOutline(rect, border);

            var toggleRect = new Rect(rect.x + 14f, rect.y + (rect.height - ToggleBoxSize) * 0.5f, ToggleBoxSize, ToggleBoxSize);
            var textLeft   = toggleRect.xMax + 12f;
            var statusWidth= 100f;
            var titleRect  = new Rect(textLeft, rect.y + 8f,  rect.width - (textLeft - rect.x) - statusWidth - 14f, 20f);
            var descRect   = new Rect(textLeft, rect.y + 28f, rect.width - (textLeft - rect.x) - statusWidth - 14f, rect.height - 32f);
            var statusRect = new Rect(rect.xMax - statusWidth - 12f, rect.y, statusWidth, rect.height);

            using (new EditorGUI.DisabledScope(isInstalled))
            {
                var next = EditorGUI.Toggle(toggleRect, isChecked);
                if (!isInstalled && next != isChecked)
                {
                    if (next) _selected.Add(pkg.Id);
                    else _selected.Remove(pkg.Id);
                }
            }

            EditorGUI.LabelField(titleRect,  pkg.DisplayName ?? pkg.Id,       TitleStyle);
            EditorGUI.LabelField(descRect,   pkg.Description ?? string.Empty, DescStyle);

            var statusText  = isInstalled ? "Installed" : pkg.Recommended ? "Recommended" : "Optional";
            var statusStyle = isInstalled ? InstalledStatusStyle : pkg.Recommended ? RecommendedStatusStyle : OptionalStatusStyle;
            EditorGUI.LabelField(statusRect, statusText, statusStyle);

            if (!isInstalled && Event.current.type == EventType.MouseDown
                && rect.Contains(Event.current.mousePosition)
                && !toggleRect.Contains(Event.current.mousePosition))
            {
                if (_selected.Contains(pkg.Id)) _selected.Remove(pkg.Id);
                else _selected.Add(pkg.Id);
                GUI.FocusControl(null);
                Event.current.Use();
            }
        }

        private void DrawImportButton()
        {
            var hasSelection = _selected.Count > 0;
            var prevBg = GUI.backgroundColor;
            if (hasSelection) GUI.backgroundColor = AccentBlue;

            using (new EditorGUI.DisabledScope(!hasSelection))
            {
                var label = hasSelection
                    ? $"Import {_selected.Count} package{(_selected.Count > 1 ? "s" : "")}"
                    : "Import Selected";
                if (GUILayout.Button(label, GUILayout.Height(ImportButtonHeight)))
                    PerformImportSelected();
            }

            GUI.backgroundColor = prevBg;
        }

        private void PerformImportSelected()
        {
            var ids = _selected.ToList();
            if (ids.Count == 0) return;

            var installed = _performInstall(ids);
            _selected.Clear();
            RefreshState();

            if (installed > 0)
                UnityEngine.Debug.Log(
                    $"[{_logTag}] Imported {installed} {GroupTitle} template(s). " +
                    "Unity will recompile and the remaining configuration will be applied automatically.");
        }

        private static void BeginCard()
        {
            var rect = EditorGUILayout.BeginVertical();
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, CardBg);
                DrawOutline(rect, CardBorder);
            }
            GUILayout.Space(CardPad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(CardPad);
            EditorGUILayout.BeginVertical();
        }

        private static void EndCard()
        {
            EditorGUILayout.EndVertical();
            GUILayout.Space(CardPad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(CardPad);
            EditorGUILayout.EndVertical();
        }

        private static void DrawHeader(string title)
        {
            GUILayout.Label(title, HeaderStyle);
            GUILayout.Space(3f);
            var line = GUILayoutUtility.GetRect(0f, 2f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(line, new Color(0.30f, 0.30f, 0.30f));
            GUILayout.Space(10f);
        }

        private static void DrawOutline(Rect r, Color c)
        {
            EditorGUI.DrawRect(new Rect(r.x,         r.y,         r.width, 1f),       c);
            EditorGUI.DrawRect(new Rect(r.x,         r.yMax - 1f, r.width, 1f),       c);
            EditorGUI.DrawRect(new Rect(r.x,         r.y,         1f,      r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - 1f, r.y,         1f,      r.height), c);
        }

        private static GUIStyle _headerStyle;
        private static GUIStyle HeaderStyle => _headerStyle ??= new GUIStyle(EditorStyles.boldLabel)
        { fontSize = 13, normal = { textColor = HeaderTextColor } };

        private static GUIStyle _introStyle;
        private static GUIStyle IntroStyle => _introStyle ??= new GUIStyle(EditorStyles.label)
        { fontSize = 12, wordWrap = true, normal = { textColor = DimTextColor } };

        private static GUIStyle _titleStyle;
        private static GUIStyle TitleStyle => _titleStyle ??= new GUIStyle(EditorStyles.boldLabel)
        { fontSize = 13, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.92f, 0.92f, 0.92f) } };

        private static GUIStyle _descStyle;
        private static GUIStyle DescStyle => _descStyle ??= new GUIStyle(EditorStyles.miniLabel)
        { fontSize = 11, wordWrap = true, alignment = TextAnchor.UpperLeft, normal = { textColor = DimTextColor } };

        private static GUIStyle _installedStatusStyle;
        private static GUIStyle InstalledStatusStyle => _installedStatusStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
        { alignment = TextAnchor.MiddleRight, normal = { textColor = OkTextColor } };

        private static GUIStyle _recommendedStatusStyle;
        private static GUIStyle RecommendedStatusStyle => _recommendedStatusStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
        { alignment = TextAnchor.MiddleRight, normal = { textColor = AccentBlue } };

        private static GUIStyle _optionalStatusStyle;
        private static GUIStyle OptionalStatusStyle => _optionalStatusStyle ??= new GUIStyle(EditorStyles.miniLabel)
        { alignment = TextAnchor.MiddleRight, normal = { textColor = DimTextColor } };

        private static GUIStyle _selectAllLabelStyle;
        private static GUIStyle SelectAllLabelStyle => _selectAllLabelStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
        { fontSize = 11, alignment = TextAnchor.MiddleLeft, normal = { textColor = SelectAllTextColor } };

        private static GUIStyle _selectAllCountStyle;
        private static GUIStyle SelectAllCountStyle => _selectAllCountStyle ??= new GUIStyle(EditorStyles.miniLabel)
        { fontSize = 11, alignment = TextAnchor.MiddleRight, normal = { textColor = DimTextColor } };
    }
}
