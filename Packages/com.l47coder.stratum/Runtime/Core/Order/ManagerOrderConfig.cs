using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratum
{
    public class ManagerOrderConfig : ScriptableObject
    {
        [SerializeField] private List<ManagerOrderEntry> _entries = new();
        public List<ManagerOrderEntry> Entries => _entries;
    }

    [Serializable]
    public class ManagerOrderEntry
    {
        [TableColumn(Title = "Manager", Readonly = true)]
        public string Name;

        [TableColumn(Hide = true)]
        public string AssemblyQualifiedName;
    }
}
