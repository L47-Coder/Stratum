#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TableView
    {
        private readonly struct ColumnDefinition
        {
            public readonly string Header;
            public readonly string RelativePropertyPath;
            public readonly bool Editable;
            public readonly FieldInfo Field;
            public readonly float MinWidth;
            public readonly float InitialPreferredWidth;
            public readonly string DropdownMethodName;

            public ColumnDefinition(string header, string relPath, bool editable, FieldInfo field, float minWidth, float initialPreferredWidth)
            {
                Header = header;
                RelativePropertyPath = relPath;
                Editable = editable;
                Field = field;
                MinWidth = minWidth;
                InitialPreferredWidth = initialPreferredWidth;
                DropdownMethodName = field?.GetCustomAttribute<DropdownAttribute>(false)?.MethodName;
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
            var attr = field.GetCustomAttribute<TableColumnAttribute>(false);
            if (attr != null && !attr.Visible) return null;

            var header = string.IsNullOrWhiteSpace(attr?.Header)
                ? ObjectNames.NicifyVariableName(field.Name)
                : attr.Header;
            var initial = attr != null && attr.InitialWidth > 0f ? attr.InitialWidth : 0f;
            return new ColumnDefinition(
                header,
                field.Name,
                attr?.Editable ?? true,
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
#endif
