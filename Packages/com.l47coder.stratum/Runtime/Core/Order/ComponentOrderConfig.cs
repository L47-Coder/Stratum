using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratum
{
    public class ComponentOrderConfig : ScriptableObject
    {
        [SerializeField] private List<ComponentOrderEntry> _entries = new();
        public List<ComponentOrderEntry> Entries => _entries;
    }

    [Serializable]
    public class ComponentOrderEntry
    {
        [TableColumn(Title = "Component", Readonly = true)]
        public string Name;

        [TableColumn(Hide = true)]
        public string AssemblyQualifiedName;
    }
}
