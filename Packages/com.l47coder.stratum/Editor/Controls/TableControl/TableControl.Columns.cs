using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TableControl
    {
        private readonly struct ColumnDefinition
        {
            public readonly string Title;
            public readonly string RelativePropertyPath;
            public readonly bool Readonly;
            public readonly FieldInfo Field;
            public readonly float MinWidth;
            public readonly float InitialPreferredWidth;
            public readonly string DropdownMethodName;

            public ColumnDefinition(string title, string relPath, bool @readonly, FieldInfo field, float minWidth, float initialPreferredWidth)
            {
                Title = title;
                RelativePropertyPath = relPath;
                Readonly = @readonly;
                Field = field;
                MinWidth = minWidth;
                InitialPreferredWidth = initialPreferredWidth;
                DropdownMethodName = field?.GetCustomAttribute<FieldAttribute>(false)?.Dropdown;
            }
        }

        private static List<ColumnDefinition> BuildColumnsFromElementType(Type elementType)
        {
            if (elementType == null) return new List<ColumnDefinition>();

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            return elementType
                .GetFields(flags)
                .Where(IsSerializedField)
                .OrderBy(f => f.MetadataToken)
                .Select(ToColumnDefinition)
                .Where(c => c.HasValue)
                .Select(c => c.Value)
                .ToList();
        }

        private static ColumnDefinition? ToColumnDefinition(FieldInfo field)
        {
            var attr = field.GetCustomAttribute<FieldAttribute>(false);
            if (attr != null && attr.Hide) return null;

            var title = string.IsNullOrWhiteSpace(attr?.Title)
                ? ObjectNames.NicifyVariableName(field.Name)
                : attr.Title;
            var initial = attr != null && attr.Width > 0f ? attr.Width : 0f;
            return new ColumnDefinition(
                title,
                field.Name,
                attr?.Readonly ?? false,
                field,
                GetDefaultMinWidth(field.FieldType),
                initial);
        }

        private static bool IsSerializedField(FieldInfo field) =>
            !field.IsStatic &&
            !field.IsDefined(typeof(NonSerializedAttribute), false) &&
            !field.IsDefined(typeof(HideInInspector), false) &&
            (field.IsPublic || field.IsDefined(typeof(SerializeField), false));

        private static float GetDefaultMinWidth(Type type)
        {
            if (type == typeof(bool)) return 40f;
            if (type == typeof(int) || type == typeof(float) || type.IsEnum) return 120f;
            if (type == typeof(Color)) return 120f;
            if (type == typeof(string)) return 140f;
            if (type == typeof(LayerMask)) return 120f;
            if (type == typeof(AnimationCurve) || type == typeof(Gradient)) return 120f;
            if (IsStringList(type)) return 120f;
            if (type == typeof(Vector2) || type == typeof(Vector2Int)) return 140f;
            if (type == typeof(Vector3) || type == typeof(Vector3Int) || type == typeof(Quaternion)) return 210f;
            if (type == typeof(Vector4)) return 280f;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return 140f;
            return DefaultFallbackMinWidth;
        }

        private void EnsureColumnSizing()
        {
            var count = _columns.Count;
            if (_columnMinWidths == null || _columnMinWidths.Length != count)
            {
                _columnMinWidths = new float[count];
                for (var i = 0; i < count; i++) _columnMinWidths[i] = _columns[i].MinWidth;
            }
            if (_columnPreferredWidths == null || _columnPreferredWidths.Length != count)
            {
                _columnPreferredWidths = new float[count];
                for (var i = 0; i < count; i++)
                    _columnPreferredWidths[i] = Mathf.Max(_columnMinWidths[i],
                        _columns[i].InitialPreferredWidth > 0f ? _columns[i].InitialPreferredWidth : _columnMinWidths[i]);
            }
        }
    }
}
