using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class FieldPopup
    {
        private Action _onChanged;

        private const float PopupW = 360f;
        private const float PaddingV = 4f;
        private const float MaxHeight = 420f;

        private void ShowCore<T>(Rect anchorRect, T item)
        {
            PopupWindow.Show(anchorRect, new PopupContent<T>(item, Readonly, _onChanged));
        }

        private sealed class PopupContent<T> : PopupWindowContent
        {
            private readonly FieldRenderer _renderer;
            private readonly Action _onChanged;
            private readonly float _contentH;

            internal PopupContent(T item, bool @readonly, Action onChanged)
            {
                _renderer = new FieldRenderer { Readonly = @readonly };
                _onChanged = onChanged;
                var count = _renderer.Prepare(item);
                _contentH = count > 0 ? _renderer.ContentHeight() : 22f;
            }

            public override Vector2 GetWindowSize() =>
                new(PopupW, Mathf.Min(_contentH + PaddingV * 2f, MaxHeight));

            public override void OnGUI(Rect rect)
            {
                var inner = new Rect(rect.x, rect.y + PaddingV, rect.width, rect.height - PaddingV * 2f);
                _renderer.DrawRows(inner);
            }

            // Popup 关闭时一次性把草稿合并回原对象。提交时机完全确定，
            // 不依赖 IMGUI 的失焦/重绘时机。
            public override void OnClose()
            {
                if (_renderer.Commit())
                    _onChanged?.Invoke();
            }
        }

        private sealed class FieldRenderer
        {
            private const float LabelWidth = 130f;
            private const float RowHeight = 22f;
            private const float RowGap = 2f;
            private const float PaddingH = 4f;
            private const float PaddingV = 2f;

            private static readonly Color RowBg0 = new(0.19f, 0.19f, 0.20f, 1f);
            private static readonly Color RowBg1 = new(0.21f, 0.21f, 0.22f, 1f);
            private static readonly Color DividerColor = new(0.14f, 0.14f, 0.15f, 1f);

            internal bool Readonly { get; set; }

            private Type _cachedType;
            private List<FieldDef> _fieldDefs;
            private Vector2 _scrollPos;
            private object _lastItem;

            // 草稿层：值类型/string/UnityObject/Enum 等"应当延迟提交"的字段在编辑期
            // 只写入 _draft，关闭 Popup 时再合并回 _lastItem。绕开 IMGUI 全局
            // RecycledTextEditor 的失焦/重绘时机不可控问题。
            private Dictionary<FieldInfo, object> _draft;

            internal int Prepare<T>(T item)
            {
                if (item == null) { _lastItem = null; _draft = null; return 0; }
                _lastItem = item;
                var t = item.GetType();
                if (_cachedType != t) { _cachedType = t; _fieldDefs = null; }
                _fieldDefs ??= BuildFieldDefs(t);

                _draft = new Dictionary<FieldInfo, object>(_fieldDefs.Count);
                foreach (var def in _fieldDefs)
                    if (IsDraftManaged(def.Field.FieldType))
                        _draft[def.Field] = def.Field.GetValue(item);

                return _fieldDefs.Count;
            }

            // 把草稿一次性合并回 _lastItem。返回是否实际有任何字段发生变化。
            internal bool Commit()
            {
                if (_lastItem == null || _draft == null) return false;
                var any = false;
                foreach (var kv in _draft)
                {
                    var orig = kv.Key.GetValue(_lastItem);
                    if (Equals(orig, kv.Value)) continue;
                    kv.Key.SetValue(_lastItem, kv.Value);
                    any = true;
                }
                return any;
            }

            // 引用类型 + 用户自管编辑（AnimationCurve/Gradient/StringList）保持 in-place，
            // 它们没有"延迟提交"问题，也不希望走草稿（草稿要做深拷贝才安全）。
            private static bool IsDraftManaged(Type t)
            {
                if (IsStringList(t)) return false;
                if (t == typeof(AnimationCurve) || t == typeof(Gradient)) return false;
                return true;
            }

            internal float ContentHeight() =>
                _fieldDefs == null || _fieldDefs.Count == 0 ? 0f
                : _fieldDefs.Count * (RowHeight + RowGap) - RowGap;

            internal void DrawRows(Rect contentRect)
            {
                if (_fieldDefs == null || _lastItem == null) return;

                var totalH = ContentHeight();
                var needScroll = totalH > contentRect.height;
                var viewW = contentRect.width - (needScroll ? ControlsToolbar.VerticalScrollbarWidth : 0f);
                var viewRect = new Rect(0f, 0f, viewW, Mathf.Max(totalH, contentRect.height));

                GUI.BeginGroup(contentRect);
                _scrollPos = GUI.BeginScrollView(
                    new Rect(0f, 0f, contentRect.width, contentRect.height),
                    _scrollPos, viewRect, false, needScroll);

                var boxed = _lastItem;
                var y = 0f;
                for (var i = 0; i < _fieldDefs.Count; i++)
                {
                    DrawFieldRow(new Rect(0f, y, viewW, RowHeight), ref boxed, _fieldDefs[i]);
                    y += RowHeight + RowGap;
                }

                GUI.EndScrollView();
                GUI.EndGroup();
            }

            private void DrawFieldRow(Rect rowRect, ref object boxed, FieldDef def)
            {
                var stripe = _fieldDefs.IndexOf(def) % 2;
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(rowRect, stripe == 0 ? RowBg0 : RowBg1);

                var divX = rowRect.x + LabelWidth;
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(new Rect(divX, rowRect.y, 1f, rowRect.height), DividerColor);

                var labelRect = new Rect(rowRect.x + PaddingH, rowRect.y + PaddingV, LabelWidth - PaddingH * 2f, rowRect.height - PaddingV * 2f);
                var controlRect = new Rect(divX + 1f + PaddingH, rowRect.y + PaddingV, rowRect.xMax - divX - 1f - PaddingH * 2f, rowRect.height - PaddingV * 2f);

                GUI.Label(labelRect, def.Title, LabelStyle);
                using (new EditorGUI.DisabledScope(Readonly || def.Readonly))
                    DrawFieldControl(controlRect, ref boxed, def);
            }

            private void DrawFieldControl(Rect rect, ref object boxed, FieldDef def)
            {
                var field = def.Field;
                var type = field.FieldType;
                var managed = IsDraftManaged(type);
                // 草稿管理字段从 _draft 读取当前编辑值；其余引用类型从原对象读。
                var value = managed ? _draft[field] : field.GetValue(boxed);

                if (IsStringList(type))
                {
                    var list = value as List<string> ?? new List<string>();
                    var summary = list.Count == 0 ? "(空)" : $"[{list.Count}]  {string.Join(", ", list)}";
                    EditorGUI.LabelField(rect, summary, ReadonlyValueStyle);
                    return;
                }

                // String + Dropdown：用 DropdownPopup 弹泡泡，回调写入草稿；
                // 提交时机仍为 FieldPopup.OnClose() → Commit()，不直接触发 GUI.changed。
                if (type == typeof(string) && def.Dropdown != null)
                {
                    var opts = InvokeDropdownMethod(field, def.Dropdown.Method);
                    var cur  = value as string ?? string.Empty;
                    if (opts is { Length: > 0 })
                    {
                        if (GUI.Button(rect, string.IsNullOrEmpty(cur) ? "(未选择)" : cur, DropdownButtonStyle))
                        {
                            var captDraft = _draft;
                            var captField = field;
                            var popup = new DropdownPopup
                            {
                                Multi     = def.Dropdown.Multi,
                                Separator = def.Dropdown.Separator,
                            };
                            popup.OnConfirmed(finalValue => captDraft[captField] = finalValue);
                            popup.Show(rect, opts, cur);
                        }
                    }
                    else
                        EditorGUI.LabelField(rect, cur, ReadonlyValueStyle);
                    return;
                }

                var supported =
                    type == typeof(string) || type == typeof(int) || type == typeof(float) ||
                    type == typeof(bool) || type.IsEnum ||
                    typeof(UnityEngine.Object).IsAssignableFrom(type) ||
                    type == typeof(AnimationCurve) || type == typeof(Gradient) ||
                    type == typeof(Color) ||
                    type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
                    type == typeof(Vector2Int) || type == typeof(Vector3Int) || type == typeof(Quaternion) ||
                    type == typeof(LayerMask);

                if (!supported) { EditorGUI.LabelField(rect, value?.ToString() ?? "null", ReadonlyValueStyle); return; }

                EditorGUI.BeginChangeCheck();
                object newValue;

                if (type == typeof(string))
                {
                    newValue = EditorGUI.TextField(rect, value as string ?? string.Empty);
                }
                else if (type == typeof(int)) newValue = EditorGUI.IntField(rect, value is int iv ? iv : 0);
                else if (type == typeof(float)) newValue = EditorGUI.FloatField(rect, value is float fv ? fv : 0f);
                else if (type == typeof(bool)) newValue = DrawToggle(rect, value is bool bv && bv);
                else if (type.IsEnum) newValue = EditorGUI.EnumPopup(rect, (Enum)value);
                else if (type == typeof(AnimationCurve)) newValue = EditorGUI.CurveField(rect, value as AnimationCurve ?? new AnimationCurve());
                else if (type == typeof(Gradient)) newValue = EditorGUI.GradientField(rect, value as Gradient ?? new Gradient());
                else if (type == typeof(Color)) newValue = EditorGUI.ColorField(rect, value is Color cv ? cv : Color.white);
                else if (type == typeof(Vector2)) newValue = EditorGUI.Vector2Field(rect, GUIContent.none, value is Vector2 v2 ? v2 : default);
                else if (type == typeof(Vector3)) newValue = EditorGUI.Vector3Field(rect, GUIContent.none, value is Vector3 v3 ? v3 : default);
                else if (type == typeof(Vector4)) newValue = EditorGUI.Vector4Field(rect, GUIContent.none, value is Vector4 v4 ? v4 : default);
                else if (type == typeof(Vector2Int)) newValue = EditorGUI.Vector2IntField(rect, GUIContent.none, value is Vector2Int vi2 ? vi2 : default);
                else if (type == typeof(Vector3Int)) newValue = EditorGUI.Vector3IntField(rect, GUIContent.none, value is Vector3Int vi3 ? vi3 : default);
                else if (type == typeof(Quaternion))
                {
                    var q = value is Quaternion qv ? qv : Quaternion.identity;
                    newValue = Quaternion.Euler(EditorGUI.Vector3Field(rect, GUIContent.none, q.eulerAngles));
                }
                else if (type == typeof(LayerMask))
                {
                    var mask = value is LayerMask lm ? lm : default;
                    var concat = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(mask);
                    newValue = (LayerMask)(int)InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(EditorGUI.MaskField(rect, concat, InternalEditorUtility.layers));
                }
                else newValue = EditorGUI.ObjectField(rect, value as UnityEngine.Object, type, true);

                if (!EditorGUI.EndChangeCheck()) return;
                if (managed) _draft[field] = newValue;
                else field.SetValue(boxed, newValue);
                GUI.changed = true;
            }

            private static readonly Color ToggleOnColor = new(0.22f, 0.62f, 0.35f, 0.88f);
            private static readonly Color ToggleOffColor = new(0.72f, 0.22f, 0.22f, 0.88f);

            private static GUIStyle _toggleStyle;
            private static GUIStyle ToggleStyle => _toggleStyle ??= new GUIStyle(EditorStyles.miniLabel)
            { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white }, fontStyle = FontStyle.Bold };

            private static bool DrawToggle(Rect rect, bool cur)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(rect, cur ? ToggleOnColor : ToggleOffColor);
                    GUI.Label(rect, cur ? "✓" : "✕", ToggleStyle);
                }
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
                { GUI.changed = true; Event.current.Use(); return !cur; }
                return cur;
            }

            private static readonly Dictionary<FieldInfo, string[]> _dropdownCache = new();

            private static string[] InvokeDropdownMethod(FieldInfo field, string methodName)
            {
                if (_dropdownCache.TryGetValue(field, out var cached)) return cached;
                var method = field.DeclaringType?.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null) { _dropdownCache[field] = Array.Empty<string>(); return null; }
                var result = method.Invoke(null, null) switch
                {
                    string[] arr => arr,
                    List<string> l => l.ToArray(),
                    IEnumerable<string> e => e.ToArray(),
                    _ => null,
                };
                _dropdownCache[field] = result ?? Array.Empty<string>();
                return result;
            }

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
                var title = string.IsNullOrWhiteSpace(attr?.Title) ? ObjectNames.NicifyVariableName(field.Name) : attr.Title;
                return new FieldDef(title, attr?.Readonly ?? false, field,
                    field.GetCustomAttribute<DropdownAttribute>(false));
            }

            private static bool IsSerializedField(FieldInfo f) =>
                !f.IsStatic &&
                !f.IsDefined(typeof(NonSerializedAttribute), false) &&
                !f.IsDefined(typeof(HideInInspector), false) &&
                (f.IsPublic || f.IsDefined(typeof(SerializeField), false));

            private static bool IsStringList(Type type) =>
                type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(List<>) &&
                type.GetGenericArguments()[0] == typeof(string);

            private static GUIStyle _dropdownButtonStyle;
            private static GUIStyle DropdownButtonStyle =>
                _dropdownButtonStyle ??= new GUIStyle(EditorStyles.popup)
                {
                    alignment = TextAnchor.MiddleLeft,
                    clipping  = TextClipping.Clip,
                };

            private static GUIStyle _labelStyle;
            private static GUIStyle LabelStyle => _labelStyle ??= new GUIStyle(EditorStyles.miniLabel)
            { alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };

            private static GUIStyle _readonlyValueStyle;
            private static GUIStyle ReadonlyValueStyle => _readonlyValueStyle ??= new GUIStyle(EditorStyles.miniLabel)
            { alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip, normal = { textColor = new Color(0.75f, 0.75f, 0.75f) } };
        }

        private readonly struct FieldDef
        {
            public readonly string Title;
            public readonly bool Readonly;
            public readonly FieldInfo Field;
            public readonly DropdownAttribute Dropdown;

            public FieldDef(string title, bool @readonly, FieldInfo field, DropdownAttribute dropdown)
            {
                Title = title;
                Readonly = @readonly;
                Field = field;
                Dropdown = dropdown;
            }
        }
    }
}
