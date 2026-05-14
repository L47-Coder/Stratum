using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class SoDetailWindow : EditorWindow
    {
        private const float TabBarHeight = 26f;
        private const float TabWidth = 160f;
        private const float TabCloseWidth = 18f;
        private const float TabPadding = 1f;

        private static readonly Color TabBarBg = new(0.14f, 0.14f, 0.14f);
        private static readonly Color TabActiveBg = new(0.22f, 0.22f, 0.22f);
        private static readonly Color TabInactiveBg = new(0.18f, 0.18f, 0.18f);
        private static readonly Color TabHoverBg = new(0.20f, 0.20f, 0.20f);
        private static readonly Color TabAccent = new(0.35f, 0.65f, 1f);
        private static readonly Color BodyBg = new(0.20f, 0.20f, 0.20f);
        private static readonly Color DividerCol = new(0.11f, 0.11f, 0.11f);
        private static readonly Color DimText = new(0.70f, 0.70f, 0.70f);

        private static GUIStyle _tabLabelStyle;
        private static GUIStyle TabLabelStyle => _tabLabelStyle ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(8, 4, 0, 0),
            fontSize = 12,
            clipping = TextClipping.Clip,
        };

        private static GUIStyle _tabCloseStyle;
        private static GUIStyle TabCloseStyle => _tabCloseStyle ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            normal = { textColor = DimText },
        };

        private static GUIStyle _emptyStyle;
        private static GUIStyle EmptyStyle => _emptyStyle ??= new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            fontSize = 12,
        };

        [SerializeField] private List<string> _persistedGuids = new();
        [SerializeField] private int _persistedActiveIndex = -1;

        private readonly List<TabState> _tabs = new();
        private int _activeIndex = -1;
        private Vector2 _bodyScroll;
        private float _tabBarScrollX;

        private sealed class TabState
        {
            public string Guid;
            public ScriptableObject Asset;
            public UnityEditor.Editor Editor;
        }

        public static void OpenAsset(ScriptableObject asset)
        {
            if (asset == null) return;
            var win = GetOrCreate();
            win.OpenOrFocusTab(asset);
        }

        public static void CloseAsset(ScriptableObject asset)
        {
            if (asset == null) return;
            var win = TryFindOpen();
            if (win == null) return;
            var path = AssetDatabase.GetAssetPath(asset);
            var guid = string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) return;
            win.CloseTabByGuid(guid);
        }

        public static void CloseAssetByPath(string assetPath)
        {
            var win = TryFindOpen();
            if (win == null || string.IsNullOrEmpty(assetPath)) return;
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) return;
            win.CloseTabByGuid(guid);
        }

        private static SoDetailWindow GetOrCreate()
        {
            var win = GetWindow<SoDetailWindow>("SO Detail");
            win.minSize = new Vector2(360f, 240f);
            return win;
        }

        private static SoDetailWindow TryFindOpen()
        {
            var windows = Resources.FindObjectsOfTypeAll<SoDetailWindow>();
            return windows.Length > 0 ? windows[0] : null;
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("SO Detail");
            RehydrateFromPersistedGuids();
        }

        private void OnDisable()
        {
            FlushPersistedState();
            foreach (var t in _tabs)
                if (t.Editor != null) DestroyImmediate(t.Editor);
            _tabs.Clear();
            _activeIndex = -1;
        }

        private void OpenOrFocusTab(ScriptableObject asset)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath)) return;
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) return;

            var existing = _tabs.FindIndex(t => t.Guid == guid);
            if (existing >= 0)
            {
                _activeIndex = existing;
                FlushPersistedState();
                Focus();
                Repaint();
                return;
            }

            var editor = UnityEditor.Editor.CreateEditor(asset);
            _tabs.Add(new TabState { Guid = guid, Asset = asset, Editor = editor });
            _activeIndex = _tabs.Count - 1;
            FlushPersistedState();
            Focus();
            Repaint();
        }

        private void CloseTabByGuid(string guid)
        {
            var index = _tabs.FindIndex(t => t.Guid == guid);
            if (index < 0) return;
            CloseTabAt(index);
        }

        private void CloseTabAt(int index)
        {
            if (index < 0 || index >= _tabs.Count) return;
            var tab = _tabs[index];
            if (tab.Editor != null) DestroyImmediate(tab.Editor);
            _tabs.RemoveAt(index);

            if (_tabs.Count == 0) _activeIndex = -1;
            else if (_activeIndex >= _tabs.Count) _activeIndex = _tabs.Count - 1;
            else if (index < _activeIndex) _activeIndex--;
            FlushPersistedState();
            Repaint();
        }

        private void RehydrateFromPersistedGuids()
        {
            _tabs.Clear();
            if (_persistedGuids == null || _persistedGuids.Count == 0)
            {
                _activeIndex = -1;
                return;
            }

            foreach (var guid in _persistedGuids)
            {
                if (string.IsNullOrEmpty(guid)) continue;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;
                _tabs.Add(new TabState
                {
                    Guid = guid,
                    Asset = asset,
                    Editor = UnityEditor.Editor.CreateEditor(asset),
                });
            }

            _activeIndex = _tabs.Count == 0
                ? -1
                : Mathf.Clamp(_persistedActiveIndex, 0, _tabs.Count - 1);
        }

        private void FlushPersistedState()
        {
            _persistedGuids ??= new List<string>();
            _persistedGuids.Clear();
            foreach (var t in _tabs) _persistedGuids.Add(t.Guid);
            _persistedActiveIndex = _activeIndex;
        }

        private void OnGUI()
        {
            DropMissingTabs();

            var fullRect = new Rect(0f, 0f, position.width, position.height);
            EditorGUI.DrawRect(fullRect, BodyBg);

            if (_tabs.Count == 0)
            {
                GUI.Label(fullRect, "No SO opened. Click a row in the SO Viewer to open.", EmptyStyle);
                return;
            }

            var tabBarRect = new Rect(0f, 0f, position.width, TabBarHeight);
            DrawTabBar(tabBarRect);
            EditorGUI.DrawRect(new Rect(0f, TabBarHeight, position.width, 1f), DividerCol);

            var bodyRect = new Rect(0f, TabBarHeight + 1f, position.width, Mathf.Max(0f, position.height - TabBarHeight - 1f));
            DrawBody(bodyRect);
        }

        private void DropMissingTabs()
        {
            for (var i = _tabs.Count - 1; i >= 0; i--)
            {
                var t = _tabs[i];
                if (t.Asset == null || string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(t.Guid)))
                    CloseTabAt(i);
            }
        }

        private void DrawTabBar(Rect rect)
        {
            EditorGUI.DrawRect(rect, TabBarBg);

            var evt = Event.current;
            var totalWidth = _tabs.Count * (TabWidth + TabPadding);
            var maxScroll = Mathf.Max(0f, totalWidth - rect.width);
            if (evt.type == EventType.ScrollWheel && rect.Contains(evt.mousePosition))
            {
                _tabBarScrollX = Mathf.Clamp(_tabBarScrollX + evt.delta.y * 20f, 0f, maxScroll);
                evt.Use();
                Repaint();
            }

            GUI.BeginClip(rect);
            try
            {
                for (var i = 0; i < _tabs.Count; i++)
                {
                    var tabRect = new Rect(i * (TabWidth + TabPadding) - _tabBarScrollX, 0f, TabWidth, rect.height);
                    if (tabRect.xMax < 0f || tabRect.x > rect.width) continue;

                    var active = i == _activeIndex;
                    var hovered = tabRect.Contains(evt.mousePosition);
                    var bg = active ? TabActiveBg : (hovered ? TabHoverBg : TabInactiveBg);
                    EditorGUI.DrawRect(tabRect, bg);
                    if (active)
                        EditorGUI.DrawRect(new Rect(tabRect.x, tabRect.yMax - 2f, tabRect.width, 2f), TabAccent);

                    var labelRect = new Rect(tabRect.x, tabRect.y, tabRect.width - TabCloseWidth, tabRect.height);
                    TabLabelStyle.normal.textColor = active ? Color.white : DimText;
                    GUI.Label(labelRect, new GUIContent(_tabs[i].Asset.name, AssetDatabase.GUIDToAssetPath(_tabs[i].Guid)), TabLabelStyle);

                    var closeRect = new Rect(tabRect.xMax - TabCloseWidth, tabRect.y, TabCloseWidth, tabRect.height);
                    GUI.Label(closeRect, "×", TabCloseStyle);

                    if (evt.type == EventType.MouseDown && evt.button == 0 && tabRect.Contains(evt.mousePosition))
                    {
                        if (closeRect.Contains(evt.mousePosition))
                        {
                            CloseTabAt(i);
                        }
                        else
                        {
                            _activeIndex = i;
                            FlushPersistedState();
                            Repaint();
                        }
                        evt.Use();
                        break;
                    }
                    if (evt.type == EventType.MouseDown && evt.button == 2 && tabRect.Contains(evt.mousePosition))
                    {
                        CloseTabAt(i);
                        evt.Use();
                        break;
                    }
                }
            }
            finally
            {
                GUI.EndClip();
            }
        }

        private void DrawBody(Rect rect)
        {
            if (_activeIndex < 0 || _activeIndex >= _tabs.Count) return;
            var tab = _tabs[_activeIndex];
            if (tab.Editor == null || tab.Asset == null) return;

            GUILayout.BeginArea(rect);
            try
            {
                _bodyScroll = EditorGUILayout.BeginScrollView(_bodyScroll);
                try
                {
                    tab.Editor.OnInspectorGUI();
                }
                finally
                {
                    EditorGUILayout.EndScrollView();
                }
            }
            finally
            {
                GUILayout.EndArea();
            }
        }
    }
}
