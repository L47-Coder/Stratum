using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class SoDetailWindow : EditorWindow
    {
        private const float StartWidth = 480f;
        private const float StartHeight = 640f;

        private static readonly Color BodyBg = new(0.20f, 0.20f, 0.20f);
        private static GUIContent _windowTitle;
        private static GUIContent WindowTitle => _windowTitle ??= new GUIContent("ScriptableObject Viewer");

        private static GUIStyle _emptyStyle;
        private static GUIStyle EmptyStyle => _emptyStyle ??= new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            fontSize = 12,
        };

        [SerializeField] private string _persistedGuid;

        private ScriptableObject _asset;
        private UnityEditor.Editor _editor;
        private Vector2 _bodyScroll;

        public static void OpenAsset(ScriptableObject asset)
        {
            if (asset == null) return;

            var alreadyOpen = HasOpenInstances<SoDetailWindow>();
            var win = GetWindow<SoDetailWindow>(false);
            win.titleContent = WindowTitle;
            win.minSize = new Vector2(320f, 240f);
            if (!alreadyOpen)
                win.position = SearchUtils.GetMainWindowCenteredPosition(new Vector2(StartWidth, StartHeight));

            win.Bind(asset);
            win.Focus();
        }

        public static void CloseAsset(ScriptableObject asset)
        {
            if (asset == null) return;
            CloseAssetByPath(AssetDatabase.GetAssetPath(asset));
        }

        public static void CloseAssetByPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !HasOpenInstances<SoDetailWindow>()) return;
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) return;
            var win = TryFindOpen();
            if (win == null || win._persistedGuid != guid) return;
            win.ClearBinding();
            win.Repaint();
        }

        private static SoDetailWindow TryFindOpen()
        {
            var windows = Resources.FindObjectsOfTypeAll<SoDetailWindow>();
            return windows.Length > 0 ? windows[0] : null;
        }

        private void OnEnable()
        {
            titleContent = WindowTitle;
            RehydrateFromPersistedGuid();
        }

        private void OnDisable() => DestroyEditor();

        private void Bind(ScriptableObject asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            var guid = string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) return;

            if (ReferenceEquals(_asset, asset) && _editor != null)
            {
                _persistedGuid = guid;
                return;
            }

            DestroyEditor();
            _asset = asset;
            _persistedGuid = guid;
            _editor = UnityEditor.Editor.CreateEditor(asset);
            _bodyScroll = Vector2.zero;
            Repaint();
        }

        private void ClearBinding()
        {
            DestroyEditor();
            _asset = null;
            _persistedGuid = null;
        }

        private void DestroyEditor()
        {
            if (_editor != null) DestroyImmediate(_editor);
            _editor = null;
        }

        private void RehydrateFromPersistedGuid()
        {
            if (string.IsNullOrEmpty(_persistedGuid)) return;
            var path = AssetDatabase.GUIDToAssetPath(_persistedGuid);
            if (string.IsNullOrEmpty(path)) { _persistedGuid = null; return; }
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset == null) { _persistedGuid = null; return; }
            _asset = asset;
            _editor = UnityEditor.Editor.CreateEditor(asset);
        }

        private void OnGUI()
        {
            var fullRect = new Rect(0f, 0f, position.width, position.height);
            EditorGUI.DrawRect(fullRect, BodyBg);

            if (_asset == null || _editor == null)
            {
                GUI.Label(fullRect, "No ScriptableObject opened. Click a row in the SO Viewer to open.", EmptyStyle);
                return;
            }

            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_asset)))
            {
                ClearBinding();
                Repaint();
                return;
            }

            GUILayout.BeginArea(fullRect);
            try
            {
                _bodyScroll = EditorGUILayout.BeginScrollView(_bodyScroll);
                try
                {
                    _editor.OnInspectorGUI();
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
