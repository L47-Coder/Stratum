using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Stratum.Editor
{    internal sealed class PageOrder : ScriptableObject
    {
        [Serializable]
        private class GroupEntry
        {
            public string Title;
            public List<string> Tabs = new();
        }

        [SerializeField] private List<GroupEntry> _groups = new();

        public List<string> GetGroupOrder() =>
            _groups.Select(g => g.Title).ToList();

        public List<string> GetTabOrder(string groupTitle) =>
            _groups.FirstOrDefault(g => g.Title == groupTitle)?.Tabs.ToList() ?? new List<string>();

        public void SetGroupOrder(List<string> titles)
        {
            var lookup = _groups.ToDictionary(g => g.Title);
            _groups = titles
                .Select(t => lookup.TryGetValue(t, out var e) ? e : new GroupEntry { Title = t })
                .ToList();
        }

        public void SetTabOrder(string groupTitle, List<string> tabs)
        {
            var group = _groups.FirstOrDefault(g => g.Title == groupTitle);
            if (group == null)
                _groups.Add(group = new GroupEntry { Title = groupTitle });
            group.Tabs = tabs;
        }
    }
}
