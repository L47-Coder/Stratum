using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed partial class DevWindow : EditorWindow
    {
        private const float StartWidth = 1000f;
        private const float StartHeight = 600f;
        private const float MenuWidth = 130f;
        private const float HeaderHeight = 25f;
        private const float DividerWidth = 1f;
        private const float MenuButtonHeight = 43f;
        private const float TabButtonWidth = 100f;

        private static readonly Color MenuBg = new(0.14f, 0.14f, 0.14f);
        private static readonly Color ContentBg = new(0.18f, 0.18f, 0.18f);
        private static readonly Color DividerCol = new(0.11f, 0.11f, 0.11f);
        private static readonly Color SelectedBg = new(0.22f, 0.22f, 0.22f);
        private static readonly Color HoverBg = new(0.18f, 0.18f, 0.18f);
        private static readonly Color Accent = new(0.35f, 0.65f, 1f);
        private static readonly Color DimText = new(0.70f, 0.70f, 0.70f);

        private static GUIStyle _menuStyle;
        private static GUIStyle MenuStyle => _menuStyle ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(16, 0, 0, 0),
            fontSize = 13,
        };

        private static GUIStyle _tabStyle;
        private static GUIStyle TabStyle => _tabStyle ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
        };

        private static DevWindow _instance;

        private sealed class PageGroup
        {
            public string Title;
            public readonly List<IPage> Pages = new();
        }

        private readonly List<PageGroup> _groups = new();
        private readonly HashSet<IPage> _initializedPages = new();
        private PageOrder _pageOrder;

        private PageGroup _currentGroup;
        private IPage _currentPage;

        private string _draggingGroupTitle;
        private string _draggingTabTitle;
        private float _menuScrollY;
        private float _tabScrollX;

        [SerializeField] private string _persistedGroupTitle;
        [SerializeField] private string _persistedTabTitle;

        [MenuItem("Tools/Stratum/Dev Workbench")]
        private static void OpenMenuItem() => Open();

        private void OnEnable()
        {
            _instance = this;
            wantsMouseMove = true;
            TryBuildPageTree();
        }

        private void OnDisable()
        {
            FrameworkSyncSettings.OnDevWindowClosed();
            _instance = null;
            _currentPage?.OnLeave();
        }

        private void TryBuildPageTree()
        {
            _pageOrder = AssetDatabase.LoadAssetAtPath<PageOrder>(WorkbenchPaths.PageOrder);
            if (_pageOrder == null) return;
            BuildPageTree();
            Repaint();
        }

        private void BuildPageTree()
        {
            var pages = new List<IPage>();
            foreach (var t in TypeCache.GetTypesDerivedFrom<IPage>().Where(t => !t.IsAbstract && !t.IsInterface))
            {
                try { if (Activator.CreateInstance(t) is IPage p) pages.Add(p); }
                catch (Exception ex) { Debug.LogWarning($"[DevWindow] Failed to instantiate {t.FullName}: {ex.Message}"); }
            }

            if (pages.Count == 0) return;

            var groupOrder = SyncOrderMap(pages.Select(p => p.GroupTitle).Distinct(), _pageOrder.GetGroupOrder(), null);
            _pageOrder.SetGroupOrder(groupOrder);

            var tabOrders = groupOrder.ToDictionary(g => g, g =>
            {
                var order = SyncOrderMap(pages.Where(p => p.GroupTitle == g).Select(p => p.TabTitle), _pageOrder.GetTabOrder(g), null);
                _pageOrder.SetTabOrder(g, order);
                return order;
            });
            EditorUtility.SetDirty(_pageOrder);
            AssetDatabase.SaveAssets();

            _groups.Clear();
            _initializedPages.Clear();
            foreach (var page in pages.OrderBy(p => groupOrder.IndexOf(p.GroupTitle)).ThenBy(p => tabOrders[p.GroupTitle].IndexOf(p.TabTitle)))
            {
                var group = _groups.FirstOrDefault(x => x.Title == page.GroupTitle);
                if (group == null) _groups.Add(group = new PageGroup { Title = page.GroupTitle });
                group.Pages.Add(page);
            }

            _currentGroup = _groups.FirstOrDefault(g => g.Title == _persistedGroupTitle) ?? _groups[0];
            _currentPage = _currentGroup.Pages.FirstOrDefault(p => p.TabTitle == _persistedTabTitle) ?? _currentGroup.Pages[0];
            _persistedGroupTitle = _currentGroup.Title;
            _persistedTabTitle = _currentPage.TabTitle;
            ActivatePage(_currentPage);
        }

        private List<string> SyncOrderMap(IEnumerable<string> activeKeys, List<string> stored, IReadOnlyList<string> defaults)
        {
            var active = activeKeys.ToList();
            var ordered = stored.Where(active.Contains).ToList();
            foreach (var key in active.Except(ordered).OrderBy(k => DefaultIndex(k)))
                ordered.Add(key);
            return ordered;

            int DefaultIndex(string key)
            {
                if (defaults == null) return int.MaxValue;
                for (var i = 0; i < defaults.Count; i++)
                    if (defaults[i] == key) return i;
                return int.MaxValue;
            }
        }

        private void ActivatePage(IPage page)
        {
            if (_initializedPages.Add(page)) page.OnFirstEnter();
            page.OnEnter();
        }

        private void SelectPage(IPage page)
        {
            if (page == _currentPage) return;
            var group = _groups.FirstOrDefault(g => g.Pages.Contains(page));
            if (group == null) return;

            if (group != _currentGroup) _tabScrollX = 0f;
            _currentPage?.OnLeave();
            _currentGroup = group;
            _currentPage = page;
            _persistedGroupTitle = _currentGroup.Title;
            _persistedTabTitle = _currentPage.TabTitle;
            ActivatePage(page);
        }

        private void OnGUI()
        {
            if (_groups.Count == 0) return;

            var contentX = MenuWidth + DividerWidth;
            var contentW = Mathf.Max(0f, position.width - contentX);
            var menuRect = new Rect(0f, 0f, MenuWidth, position.height);
            var headerRect = new Rect(contentX, 0f, contentW, HeaderHeight);
            var bodyRect = new Rect(contentX, HeaderHeight + DividerWidth, contentW, Mathf.Max(0f, position.height - HeaderHeight - DividerWidth));

            EditorGUI.DrawRect(menuRect, MenuBg);
            DrawMenuItems(menuRect);
            EditorGUI.DrawRect(new Rect(MenuWidth, 0f, DividerWidth, position.height), DividerCol);

            EditorGUI.DrawRect(headerRect, MenuBg);
            DrawTabs(headerRect);
            EditorGUI.DrawRect(new Rect(contentX, HeaderHeight, contentW, DividerWidth), DividerCol);
            EditorGUI.DrawRect(bodyRect, ContentBg);
            _currentPage.OnGUI(bodyRect);

            if (Event.current.type is EventType.MouseMove or EventType.MouseEnterWindow or EventType.MouseLeaveWindow)
                Repaint();
        }

        private void DrawMenuItems(Rect rect)
        {
            var evt = Event.current;
            var totalHeight = _groups.Count * MenuButtonHeight;
            var maxScroll = Mathf.Max(0f, totalHeight - rect.height);

            if (evt.type == EventType.ScrollWheel && rect.Contains(evt.mousePosition))
            {
                _menuScrollY = Mathf.Clamp(_menuScrollY + evt.delta.y * 20f, 0f, maxScroll);
                evt.Use();
                Repaint();
            }

            GUI.BeginClip(rect);
            var dragSwapped = false;
            for (var i = 0; i < _groups.Count && !dragSwapped; i++)
            {
                var group = _groups[i];
                var btnRect = new Rect(0f, i * MenuButtonHeight - _menuScrollY, rect.width, MenuButtonHeight);
                if (btnRect.yMax < 0f || btnRect.y > rect.height) continue;

                var selected = group == _currentGroup;
                var hovered = btnRect.Contains(evt.mousePosition);

                if (selected)
                {
                    EditorGUI.DrawRect(btnRect, SelectedBg);
                    EditorGUI.DrawRect(new Rect(btnRect.x, btnRect.y, 4f, btnRect.height), Accent);
                }
                else if (hovered) EditorGUI.DrawRect(btnRect, HoverBg);

                MenuStyle.normal.textColor = selected ? Color.white : DimText;
                GUI.Label(btnRect, group.Title, MenuStyle);

                switch (evt.type)
                {
                    case EventType.MouseDown when evt.button == 0 && hovered:
                        if (!selected) SelectPage(group.Pages[0]);
                        _draggingGroupTitle = group.Title;
                        evt.Use();
                        break;
                    case EventType.MouseDrag when _draggingGroupTitle != null && hovered && group.Title != _draggingGroupTitle:
                        SwapOrder(
                            _pageOrder.GetGroupOrder(), _draggingGroupTitle, group.Title,
                            list => { _pageOrder.SetGroupOrder(list); _groups.Sort((x, y) => list.IndexOf(x.Title).CompareTo(list.IndexOf(y.Title))); });
                        evt.Use();
                        dragSwapped = true;
                        break;
                }
            }
            GUI.EndClip();

            if (!dragSwapped && evt.rawType == EventType.MouseUp) _draggingGroupTitle = null;
        }

        private void DrawTabs(Rect rect)
        {
            var evt = Event.current;
            var tabs = _currentGroup.Pages;
            var tabWidth = TabButtonWidth;
            var totalWidth = tabs.Count * tabWidth;
            var maxScroll = Mathf.Max(0f, totalWidth - rect.width);

            if (evt.type == EventType.ScrollWheel && rect.Contains(evt.mousePosition))
            {
                _tabScrollX = Mathf.Clamp(_tabScrollX + evt.delta.y * 20f, 0f, maxScroll);
                evt.Use();
                Repaint();
            }

            GUI.BeginClip(rect);
            var dragSwapped = false;
            for (var i = 0; i < tabs.Count && !dragSwapped; i++)
            {
                var page = tabs[i];
                var tabRect = new Rect(i * tabWidth - _tabScrollX, 0f, tabWidth, rect.height);
                if (tabRect.xMax < 0f || tabRect.x > rect.width) continue;

                var selected = page == _currentPage;
                var hovered = tabRect.Contains(evt.mousePosition);

                if (selected)
                {
                    EditorGUI.DrawRect(tabRect, SelectedBg);
                    EditorGUI.DrawRect(new Rect(tabRect.x, tabRect.yMax - 2f, tabRect.width, 2f), Accent);
                }
                else if (hovered) EditorGUI.DrawRect(tabRect, HoverBg);

                TabStyle.normal.textColor = selected ? Color.white : DimText;
                GUI.Label(tabRect, page.TabTitle, TabStyle);

                switch (evt.type)
                {
                    case EventType.MouseDown when evt.button == 0 && hovered:
                        SelectPage(page);
                        _draggingTabTitle = page.TabTitle;
                        evt.Use();
                        break;
                    case EventType.MouseDrag when _draggingTabTitle != null && hovered && page.TabTitle != _draggingTabTitle:
                        SwapOrder(
                            _pageOrder.GetTabOrder(_currentGroup.Title), _draggingTabTitle, page.TabTitle,
                            list => { _pageOrder.SetTabOrder(_currentGroup.Title, list); _currentGroup.Pages.Sort((x, y) => list.IndexOf(x.TabTitle).CompareTo(list.IndexOf(y.TabTitle))); });
                        evt.Use();
                        dragSwapped = true;
                        break;
                }
            }
            GUI.EndClip();

            if (!dragSwapped && evt.rawType == EventType.MouseUp) _draggingTabTitle = null;
        }

        private void SwapOrder(List<string> list, string a, string b, Action<List<string>> commit)
        {
            var ia = list.IndexOf(a);
            var ib = list.IndexOf(b);
            if (ia < 0 || ib < 0) return;
            (list[ia], list[ib]) = (list[ib], list[ia]);
            commit(list);
            EditorUtility.SetDirty(_pageOrder);
            AssetDatabase.SaveAssets();
        }
    }
}
