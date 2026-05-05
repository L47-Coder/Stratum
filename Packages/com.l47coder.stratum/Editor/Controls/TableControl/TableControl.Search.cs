using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TableControl
    {
        private SearchField _searchField;
        private string _searchText = string.Empty;
        private readonly List<int> _filteredCache = new();

        private void DrawSearchBar(Rect rect)
        {
            var col = FindSearchColumn();
            var name = col.HasValue ? col.Value.Title : KeyField;
            ControlsToolbar.DrawSearchBar(rect, ref _searchField, ref _searchText,
                $"Search by {name}...", enabled: col.HasValue, disabledHint: $"No \"{name}\" column in this table");
        }

        private ColumnDefinition? FindSearchColumn() => ShowToolbar ? FindKeyColumn() : null;

        private ColumnDefinition? FindKeyColumn()
        {
            if (_columns == null || string.IsNullOrEmpty(KeyField)) return null;
            for (var i = 0; i < _columns.Count; i++)
            {
                var col = _columns[i];
                if (string.Equals(col.RelativePropertyPath, KeyField, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(col.Title, KeyField, StringComparison.OrdinalIgnoreCase))
                    return col;
            }
            return null;
        }

        private List<int> GetFilteredIndices<T>(List<T> list)
        {
            _filteredCache.Clear();
            var col = FindSearchColumn();
            if (!col.HasValue || string.IsNullOrWhiteSpace(_searchText))
            {
                for (var i = 0; i < list.Count; i++) _filteredCache.Add(i);
                return _filteredCache;
            }
            var lower = _searchText.ToLowerInvariant();
            var field = col.Value.Field;
            for (var i = 0; i < list.Count; i++)
                if (GetFieldStringValue(field.GetValue(list[i])).ToLowerInvariant().Contains(lower))
                    _filteredCache.Add(i);
            return _filteredCache;
        }

        private static string GetFieldStringValue(object value) =>
            value == null ? "null" : value switch
            {
                string s => s,
                int i => i.ToString(),
                float f => f.ToString(),
                bool b => b.ToString(),
                UnityEngine.Object obj => obj ? obj.name : "null",
                _ => value.ToString()
            };

        private static string NormalizeDuplicateCompareKey(object value, Type fieldType) =>
            value == null ? string.Empty :
            fieldType == typeof(string) ? ((string)value ?? string.Empty).Trim() :
            GetFieldStringValue(value);
    }
}
