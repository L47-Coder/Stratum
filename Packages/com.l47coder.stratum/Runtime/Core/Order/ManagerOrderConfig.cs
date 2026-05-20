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
        public string Name;

        public string AssemblyQualifiedName;
    }
}
