using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class ComponentConfigPopup : EditorWindow
    {
        private const float WinW    = 340f;
        private const float MinWinH = 80f;
        private const float Padding = 8f;

        private SerializedObject   _so;
        private SerializedProperty _compProp;
        private Vector2            _scroll;

        // ── Factory ──────────────────────────────────────────────────────────

        /// <param name="so">包裹 Entity 组件的 SerializedObject。</param>
        /// <param name="componentIndex">Components 数组中的索引。</param>
        public static void Open(SerializedObject so, int componentIndex)
        {
            so.Update();
            var listProp = so.FindProperty("Components");
            if (componentIndex < 0 || componentIndex >= listProp.arraySize) return;

            var entryProp = listProp.GetArrayElementAtIndex(componentIndex);
            var dataProp  = entryProp.FindPropertyRelative("Data");
            var typeName  = dataProp.managedReferenceValue?.GetType().Name ?? "Config";

            var win = GetWindow<ComponentConfigPopup>(utility: true, title: typeName, focus: true);
            win._so       = so;
            win._compProp = dataProp;
            win.minSize   = new Vector2(WinW, MinWinH);
        }

        // ── GUI ──────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (_so == null || _compProp == null)
            {
                EditorGUILayout.HelpBox("配置数据已失效，请重新打开。", MessageType.Warning);
                return;
            }

            _so.Update();

            EditorGUILayout.Space(Padding);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            var iter = _compProp.Copy();
            var end  = _compProp.GetEndProperty();

            if (!iter.NextVisible(true) || SerializedProperty.EqualContents(iter, end))
            {
                EditorGUILayout.HelpBox("此组件配置没有可编辑字段。", MessageType.None);
            }
            else
            {
                do
                {
                    EditorGUILayout.PropertyField(iter, true);
                }
                while (iter.NextVisible(false) && !SerializedProperty.EqualContents(iter, end));
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(Padding);

            if (_so.ApplyModifiedProperties())
                AssetDatabase.SaveAssets();
        }
    }
}
