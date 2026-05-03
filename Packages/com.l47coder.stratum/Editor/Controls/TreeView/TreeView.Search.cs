using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TreeView
    {
        private SearchField _searchField;
        private string _searchText = string.Empty;
        private string _searchNormalized = string.Empty;

        private void DrawSearchBar(Rect rect)
        {
            if (ControlsToolbar.DrawSearchBar(rect, ref _searchField, ref _searchText, _searchPlaceholder))
                _searchNormalized = NormalizeSearchFilter(_searchText);
        }

        private static string NormalizeSearchFilter(string search) =>
            string.IsNullOrWhiteSpace(search) ? string.Empty
                : new string(search.Trim().Where(static c => !char.IsWhiteSpace(c) && c != '_' && c != '-').ToArray()).ToLowerInvariant();

        private static bool MatchesFuzzySearch(string normalizedText, string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;
            if (string.IsNullOrEmpty(normalizedText)) return false;
            if (normalizedText.Contains(filter, System.StringComparison.Ordinal)) return true;
            var fi = 0;
            for (var ti = 0; ti < normalizedText.Length && fi < filter.Length; ti++)
                if (normalizedText[ti] == filter[fi]) fi++;
            return fi == filter.Length;
        }
    }
}
