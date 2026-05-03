#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class ListView
    {
        private SearchField _searchField;
        private string _searchText = string.Empty;
        private readonly List<int> _filteredCache = new();

        private void DrawSearchBar(Rect rect) =>
            ControlsToolbar.DrawSearchBar(rect, ref _searchField, ref _searchText, _searchPlaceholder);

        private List<int> GetFilteredIndices(IReadOnlyList<string> items)
        {
            _filteredCache.Clear();
            var hasSearch = !string.IsNullOrWhiteSpace(_searchText);
            var lower = hasSearch ? _searchText.ToLowerInvariant() : null;
            for (var i = 0; i < items.Count; i++)
            {
                var name = items[i] ?? string.Empty;
                if (IsIgnored(name)) continue;
                if (hasSearch && !name.ToLowerInvariant().Contains(lower)) continue;
                _filteredCache.Add(i);
            }
            return _filteredCache;
        }

        private bool IsIgnored(string name)
        {
            if (ExcludePatterns == null || ExcludePatterns.Count == 0) return false;
            foreach (var p in ExcludePatterns)
                if (ControlsToolbar.MatchesGlob(name, p)) return true;
            return false;
        }
    }
}
#endif
