using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class ManagerOrderWindow : EditorWindow
    {
        private const string WindowTitle = "Manager Order";

        private SerializedObject _serializedConfig;
        private SerializedProperty _entriesProperty;
        private ReorderableList _list;
        private Vector2 _scroll;
        private string _syncError;

        public static void Open()
        {
            var window = GetWindow<ManagerOrderWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(360f, 240f);
            window.SyncAndBind();
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(360f, 240f);
            Bind(AssetDatabase.LoadAssetAtPath<ManagerOrderConfig>(StratumPaths.ManagerOrder));
        }

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(_syncError))
                EditorGUILayout.HelpBox(_syncError, MessageType.Error);

            if (_serializedConfig == null || _serializedConfig.targetObject == null || _list == null)
            {
                EditorGUILayout.HelpBox("ManagerOrder.asset not found.", MessageType.Warning);
                if (GUILayout.Button("Initialize")) SyncAndBind();
                return;
            }

            _serializedConfig.Update();

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;
                _list.DoLayoutList();
            }

            _serializedConfig.ApplyModifiedProperties();
        }

        private void SyncAndBind()
        {
            try
            {
                _syncError = null;
                Bind(ManagerOrderSync.EnsureAndSyncAsset());
            }
            catch (Exception ex)
            {
                _syncError = ex.Message;
                Debug.LogException(ex);
                Bind(AssetDatabase.LoadAssetAtPath<ManagerOrderConfig>(StratumPaths.ManagerOrder));
            }
        }

        private void Bind(ManagerOrderConfig config)
        {
            if (config == null)
            {
                _serializedConfig = null;
                _entriesProperty = null;
                _list = null;
                return;
            }

            _serializedConfig = new SerializedObject(config);
            _entriesProperty = _serializedConfig.FindProperty("_entries");
            _list = new ReorderableList(_serializedConfig, _entriesProperty, true, true, false, false)
            {
                drawHeaderCallback = DrawHeader,
                drawElementCallback = DrawElement,
                elementHeight = EditorGUIUtility.singleLineHeight + 6f,
                onReorderCallback = _ => SaveConfig(),
            };
        }

        private static void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Managers");
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _entriesProperty.GetArrayElementAtIndex(index);
            var name = element.FindPropertyRelative("Name").stringValue;
            var assemblyQualifiedName = element.FindPropertyRelative("AssemblyQualifiedName").stringValue;

            rect.y += 3f;
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(rect, new GUIContent($"{index + 1}. {name}", assemblyQualifiedName));
        }

        private void SaveConfig()
        {
            if (_serializedConfig == null || _serializedConfig.targetObject == null) return;

            _serializedConfig.ApplyModifiedProperties();
            EditorUtility.SetDirty(_serializedConfig.targetObject);
            AssetDatabase.SaveAssets();
        }
    }
}
