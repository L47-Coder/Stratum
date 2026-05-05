using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed partial class FieldView
    {
        private const float LabelWidth = 130f;
        private const float RowHeight = 22f;
        private const float RowGap = 2f;
        private const float PaddingH = 4f;
        private const float PaddingV = 2f;

        private static readonly Color RowBg0 = new(0.19f, 0.19f, 0.20f, 1f);
        private static readonly Color RowBg1 = new(0.21f, 0.21f, 0.22f, 1f);
        private static readonly Color DividerColor = new(0.14f, 0.14f, 0.15f, 1f);

        private static GUIStyle _labelStyle;
        private static GUIStyle LabelStyle => _labelStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip,
        };

        private static GUIStyle _readonlyValueStyle;
        private static GUIStyle ReadonlyValueStyle => _readonlyValueStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip,
            normal = { textColor = new Color(0.75f, 0.75f, 0.75f) },
        };

        private readonly struct FieldDef
        {
            public readonly string Title;
            public readonly bool Readonly;
            public readonly FieldInfo Field;
            public readonly string DropdownMethodName;

            public FieldDef(string title, bool @readonly, FieldInfo field, string dropdownMethodName)
            {
                Title = title;
                Readonly = @readonly;
                Field = field;
                DropdownMethodName = dropdownMethodName;
            }
        }

        private Type _cachedType;
        private List<FieldDef> _fieldDefs;
        private Vector2 _scrollPos;
        private object _lastItem;

        private static List<FieldDef> BuildFieldDefs(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            return type.GetFields(flags)
                .Where(IsSerializedField)
                .OrderBy(f => f.MetadataToken)
                .Select(ToFieldDef)
                .Where(d => d.HasValue)
                .Select(d => d.Value)
                .ToList();
        }

        private static FieldDef? ToFieldDef(FieldInfo field)
        {
            var attr = field.GetCustomAttribute<FieldAttribute>(false);
            if (attr != null && attr.Hide) return null;

            var title = string.IsNullOrWhiteSpace(attr?.Title)
                ? ObjectNames.NicifyVariableName(field.Name)
                : attr.Title;

            return new FieldDef(
                title,
                attr?.Readonly ?? false,
                field,
                attr?.Dropdown);
        }

        private static bool IsSerializedField(FieldInfo f) =>
            !f.IsStatic &&
            !f.IsDefined(typeof(NonSerializedAttribute), false) &&
            !f.IsDefined(typeof(HideInInspector), false) &&
            (f.IsPublic || f.IsDefined(typeof(SerializeField), false));

        private void DrawFieldRow(Rect rowRect, ref object boxed, FieldDef def)
        {
            var stripeIndex = _fieldDefs.IndexOf(def) % 2;
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(rowRect, stripeIndex == 0 ? RowBg0 : RowBg1);

            var divX = rowRect.x + LabelWidth;
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(divX, rowRect.y, 1f, rowRect.height), DividerColor);

            var labelRect = new Rect(
                rowRect.x + PaddingH,
                rowRect.y + PaddingV,
                LabelWidth - PaddingH * 2f,
                rowRect.height - PaddingV * 2f);

            var controlRect = new Rect(
                divX + 1f + PaddingH,
                rowRect.y + PaddingV,
                rowRect.xMax - divX - 1f - PaddingH * 2f,
                rowRect.height - PaddingV * 2f);

            GUI.Label(labelRect, def.Title, LabelStyle);

            using (new EditorGUI.DisabledScope(Readonly || def.Readonly))
                DrawFieldControl(controlRect, ref boxed, def);
        }

        private static void DrawFieldControl(Rect rect, ref object boxed, FieldDef def)
        {
            var field = def.Field;
            var value = field.GetValue(boxed);
            var type = field.FieldType;

            if (IsStringList(type))
            {
                var list = value as List<string> ?? new List<string>();
                var summary = list.Count == 0
                    ? "(空)"
                    : $"[{list.Count}]  {string.Join(", ", list)}";
                EditorGUI.LabelField(rect, summary, ReadonlyValueStyle);
                return;
            }

            var isSupported =
                type == typeof(string) || type == typeof(int) || type == typeof(float) ||
                type == typeof(bool) || type.IsEnum ||
                typeof(UnityEngine.Object).IsAssignableFrom(type) ||
                type == typeof(AnimationCurve) || type == typeof(Gradient) ||
                type == typeof(Color) ||
                type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
                type == typeof(Vector2Int) || type == typeof(Vector3Int) || type == typeof(Quaternion) ||
                type == typeof(LayerMask);

            if (!isSupported)
            {
                EditorGUI.LabelField(rect, value?.ToString() ?? "null", ReadonlyValueStyle);
                return;
            }

            EditorGUI.BeginChangeCheck();
            object newValue;

            if (type == typeof(string))
            {
                if (def.DropdownMethodName != null)
                {
                    var options = InvokeDropdownMethod(field, def.DropdownMethodName);
                    if (options is { Length: > 0 })
                    {
                        var cur = value as string ?? string.Empty;
                        var idx = Mathf.Max(0, Array.IndexOf(options, cur));
                        newValue = options[EditorGUI.Popup(rect, idx, options)];
                    }
                    else
                        newValue = EditorGUI.DelayedTextField(rect, value as string ?? string.Empty);
                }
                else
                    newValue = EditorGUI.DelayedTextField(rect, value as string ?? string.Empty);
            }
            else if (type == typeof(int))
                newValue = EditorGUI.DelayedIntField(rect, value is int iv ? iv : 0);
            else if (type == typeof(float))
                newValue = EditorGUI.DelayedFloatField(rect, value is float fv ? fv : 0f);
            else if (type == typeof(bool))
                newValue = DrawToggleField(rect, value is bool bv && bv);
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
                var euler = EditorGUI.Vector3Field(rect, GUIContent.none, q.eulerAngles);
                newValue = Quaternion.Euler(euler);
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

            if (EditorGUI.EndChangeCheck())
            {
                field.SetValue(boxed, newValue);
                GUI.changed = true;
            }
        }

        // ── Toggle（带颜色反馈，与 TableView 保持一致）──────────────────────────

        private static readonly Color ToggleOnColor = new(0.22f, 0.62f, 0.35f, 0.88f);
        private static readonly Color ToggleOffColor = new(0.72f, 0.22f, 0.22f, 0.88f);

        private static GUIStyle _toggleStyle;
        private static GUIStyle ToggleStyle => _toggleStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Bold,
        };

        private static bool DrawToggleField(Rect rect, bool current)
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

        // ── Dropdown 反射调用（与 TableView 相同逻辑）───────────────────────────

        private static readonly Dictionary<FieldInfo, string[]> _dropdownCache = new();

        private static string[] InvokeDropdownMethod(FieldInfo field, string methodName)
        {
            if (_dropdownCache.TryGetValue(field, out var cached)) return cached;

            var method = field.DeclaringType?.GetMethod(
                methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) return null;

            var result = method.Invoke(null, null) switch
            {
                string[] arr => arr,
                List<string> list => list.ToArray(),
                IEnumerable<string> en => en.ToArray(),
                _ => null,
            };
            if (result != null) _dropdownCache[field] = result;
            return result;
        }

        // ── 工具 ─────────────────────────────────────────────────────────────────

        private static bool IsStringList(Type type) =>
            type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(List<>) &&
            type.GetGenericArguments()[0] == typeof(string);
    }
}
