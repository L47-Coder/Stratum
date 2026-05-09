using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class FieldDrawer
    {
        internal struct Options
        {
            public bool DelayedNumeric;
            public bool UnfocusOnMouseDown;
            public GUIStyle UnsupportedLabelStyle;
            public DropdownAttribute Dropdown;
            public Action<object> OnAsyncWrite;
        }

        internal static bool TryDraw(Rect rect, FieldInfo field, object value, in Options opts, out object newValue)
        {
            newValue = value;
            var type = field.FieldType;

            if (opts.UnfocusOnMouseDown &&
                Event.current.type == EventType.MouseDown &&
                rect.Contains(Event.current.mousePosition))
            {
                GUI.FocusControl(null);
            }

            if (!IsSupported(type))
            {
                EditorGUI.LabelField(rect, value?.ToString() ?? "null", opts.UnsupportedLabelStyle ?? EditorStyles.miniLabel);
                return false;
            }

            if (type == typeof(string) && opts.Dropdown != null)
            {
                DrawStringDropdown(rect, field, value, opts);
                return false;
            }

            if (type.IsEnum && !UseNativeEnumPopup(type))
            {
                var en = value is Enum ev ? ev : (Enum)Enum.ToObject(type, 0);
                DrawEnumDropdown(rect, type, en, opts);
                return false;
            }

            EditorGUI.BeginChangeCheck();

            if (type == typeof(string))
                newValue = opts.DelayedNumeric
                    ? EditorGUI.DelayedTextField(rect, value as string ?? string.Empty)
                    : EditorGUI.TextField(rect, value as string ?? string.Empty);
            else if (type == typeof(int))
                newValue = opts.DelayedNumeric
                    ? EditorGUI.DelayedIntField(rect, value is int iv ? iv : 0)
                    : EditorGUI.IntField(rect, value is int iv2 ? iv2 : 0);
            else if (type == typeof(float))
                newValue = opts.DelayedNumeric
                    ? EditorGUI.DelayedFloatField(rect, value is float fv ? fv : 0f)
                    : EditorGUI.FloatField(rect, value is float fv2 ? fv2 : 0f);
            else if (type == typeof(bool))
                newValue = DrawToggle(rect, value is bool bv && bv);
            else if (type.IsEnum)
                newValue = EditorGUI.EnumPopup(rect, (Enum)value);
            else if (type == typeof(AnimationCurve))
                newValue = EditorGUI.CurveField(rect, value as AnimationCurve ?? new AnimationCurve());
            else if (type == typeof(Gradient))
                newValue = EditorGUI.GradientField(rect, value as Gradient ?? new Gradient());
            else if (type == typeof(Color))
                newValue = EditorGUI.ColorField(rect, value is Color cv ? cv : Color.white);
            else if (type == typeof(Vector2))
                newValue = EditorGUI.Vector2Field(rect, GUIContent.none, value is Vector2 v2 ? v2 : default);
            else if (type == typeof(Vector3))
                newValue = EditorGUI.Vector3Field(rect, GUIContent.none, value is Vector3 v3 ? v3 : default);
            else if (type == typeof(Vector4))
                newValue = EditorGUI.Vector4Field(rect, GUIContent.none, value is Vector4 v4 ? v4 : default);
            else if (type == typeof(Vector2Int))
                newValue = EditorGUI.Vector2IntField(rect, GUIContent.none, value is Vector2Int vi2 ? vi2 : default);
            else if (type == typeof(Vector3Int))
                newValue = EditorGUI.Vector3IntField(rect, GUIContent.none, value is Vector3Int vi3 ? vi3 : default);
            else if (type == typeof(Quaternion))
            {
                var q = value is Quaternion qv ? qv : Quaternion.identity;
                newValue = Quaternion.Euler(EditorGUI.Vector3Field(rect, GUIContent.none, q.eulerAngles));
            }
            else if (type == typeof(LayerMask))
            {
                var mask = value is LayerMask lm ? lm : default;
                var concat = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(mask);
                var picked = EditorGUI.MaskField(rect, concat, InternalEditorUtility.layers);
                newValue = (LayerMask)(int)InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(picked);
            }
            else
                newValue = EditorGUI.ObjectField(rect, value as UnityEngine.Object, type, true);

            return EditorGUI.EndChangeCheck();
        }

        private static bool IsSupported(Type type) =>
            type == typeof(string) || type == typeof(int) || type == typeof(float) ||
            type == typeof(bool) || type.IsEnum ||
            typeof(UnityEngine.Object).IsAssignableFrom(type) ||
            type == typeof(AnimationCurve) || type == typeof(Gradient) ||
            type == typeof(Color) ||
            type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
            type == typeof(Vector2Int) || type == typeof(Vector3Int) || type == typeof(Quaternion) ||
            type == typeof(LayerMask);

        private static bool UseNativeEnumPopup(Type enumType) =>
            enumType.IsDefined(typeof(FlagsAttribute), false);

        private static void DrawStringDropdown(Rect rect, FieldInfo field, object value, in Options opts)
        {
            var cur = value as string ?? string.Empty;
            var label = string.IsNullOrEmpty(cur) ? "(未选择)" : cur;
            if (!GUI.Button(rect, label, DropdownButtonStyle)) return;

            GUI.FocusControl(null);
            var items = DropdownAttributeResolver.ResolveItems(field, opts.Dropdown.Method);
            if (items.Length == 0) return;

            var onAsync = opts.OnAsyncWrite;
            var popup = new DropdownPopup
            {
                Multi = opts.Dropdown.Multi,
                Separator = opts.Dropdown.Separator,
            };
            if (!opts.Dropdown.Search)
                popup.Search = false;
            popup.OnConfirmed(finalValue => onAsync?.Invoke(finalValue));
            popup.Show(rect, items, cur);
        }

        private static void DrawEnumDropdown(Rect rect, Type enumType, Enum current, in Options opts)
        {
            var names = Enum.GetNames(enumType);
            if (names.Length == 0) return;

            var labels = names.Select(ObjectNames.NicifyVariableName).ToArray();
            var currentName = Enum.GetName(enumType, current);
            var curLabel = currentName != null
                ? ObjectNames.NicifyVariableName(currentName)
                : current.ToString();
            var currentForPopup = currentName != null ? curLabel : labels[0];

            if (!GUI.Button(rect, curLabel, DropdownButtonStyle)) return;

            var onAsync = opts.OnAsyncWrite;
            var popup = new DropdownPopup();
            popup.OnConfirmed(selected =>
            {
                var idx = Array.IndexOf(labels, selected);
                if (idx < 0) return;
                onAsync?.Invoke(Enum.Parse(enumType, names[idx]));
            });
            popup.Show(rect, labels, currentForPopup);
        }

        private static readonly Color ToggleOnColor = new(0.22f, 0.62f, 0.35f, 0.88f);
        private static readonly Color ToggleOffColor = new(0.72f, 0.22f, 0.22f, 0.88f);

        private static bool DrawToggle(Rect rect, bool current)
        {
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, current ? ToggleOnColor : ToggleOffColor);
                GUI.Label(rect, current ? "✓" : "✕", ToggleStyle);
            }
            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
            {
                GUI.changed = true;
                Event.current.Use();
                return !current;
            }
            return current;
        }

        private static GUIStyle _toggleStyle;
        private static GUIStyle ToggleStyle => _toggleStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Bold,
        };

        private static GUIStyle _dropdownButtonStyle;
        internal static GUIStyle DropdownButtonStyle => _dropdownButtonStyle ??= new GUIStyle(EditorStyles.popup)
        {
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip,
        };
    }
}
