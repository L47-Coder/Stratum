using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    /// <summary>
    /// 以 PopupWindowContent 形式弹出的组件配置面板，无标题栏，点击外部自动关闭。
    /// </summary>
    internal sealed class ComponentConfigPopup : PopupWindowContent
    {
        private const float PopupW   = 360f;
        private const float RowH     = 22f;
        private const float RowGap   = 2f;
        private const float PaddingV = 4f;
        private const float MaxPopupH = 400f;

        private SerializedObject   _so;
        private SerializedProperty _dataProp;
        private BaseComponentData  _data;
        private int                _visibleCount;

        private readonly FieldView _fieldView = new();

        // ── Factory ──────────────────────────────────────────────────────────

        public static void Open(Rect anchorRect, SerializedObject so, int componentIndex)
        {
            so.Update();
            var listProp = so.FindProperty("Components");
            if (componentIndex < 0 || componentIndex >= listProp.arraySize) return;

            var entryProp = listProp.GetArrayElementAtIndex(componentIndex);
            var dataProp  = entryProp.FindPropertyRelative("Data");
            var dataObj   = dataProp.managedReferenceValue as BaseComponentData;

            var popup = new ComponentConfigPopup
            {
                _so           = so,
                _dataProp     = dataProp,
                _data         = dataObj,
                _visibleCount = CountVisibleFields(dataObj),
            };
            PopupWindow.Show(anchorRect, popup);
        }

        // ── PopupWindowContent ───────────────────────────────────────────────

        public override Vector2 GetWindowSize()
        {
            var bodyH = _visibleCount > 0
                ? _visibleCount * (RowH + RowGap) - RowGap
                : RowH;
            return new Vector2(PopupW, Mathf.Min(bodyH + PaddingV * 2f, MaxPopupH));
        }

        public override void OnGUI(Rect rect)
        {
            if (_so == null || _dataProp == null || _data == null)
            {
                EditorGUI.LabelField(rect, "配置数据已失效", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            var inner = new Rect(rect.x, rect.y + PaddingV, rect.width, rect.height - PaddingV * 2f);

            EditorGUI.BeginChangeCheck();
            _fieldView.DrawContent(inner, _data);
            if (EditorGUI.EndChangeCheck())
            {
                _so.Update();
                _dataProp.managedReferenceValue = _data;
                _so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }

        // ── 字段计数（供 GetWindowSize 计算高度）────────────────────────────

        private static int CountVisibleFields(BaseComponentData data)
        {
            if (data == null) return 0;

            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var count = 0;
            foreach (var f in data.GetType().GetFields(flags))
            {
                if (f.IsStatic) continue;
                if (f.IsDefined(typeof(System.NonSerializedAttribute), false)) continue;
                if (f.IsDefined(typeof(HideInInspector), false)) continue;
                if (!f.IsPublic && !f.IsDefined(typeof(UnityEngine.SerializeField), false)) continue;
                var attr = f.GetCustomAttribute<FieldAttribute>(false);
                if (attr != null && attr.Hide) continue;
                count++;
            }
            return count;
        }
    }
}
