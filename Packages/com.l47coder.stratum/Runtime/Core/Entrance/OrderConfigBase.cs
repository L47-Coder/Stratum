using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevWorkbench
{
    public abstract class OrderConfigBase<TEntry> : ScriptableObject
        where TEntry : class
    {
        [SerializeField]
        private List<TEntry> _entries = new();
        public List<TEntry> Entries => _entries;
    }

    [Serializable]
    public abstract class OrderEntryBase : ITableViewItem
    {
        [TableColumn(Visible = false)]
        public string AssemblyQualifiedName;
    }
}
