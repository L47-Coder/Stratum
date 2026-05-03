using System;
using UnityEngine;

namespace DevWorkbench
{
    public class ManagerOrderConfig : OrderConfigBase<ManagerOrderEntry> { }

    [Serializable]
    public class ManagerOrderEntry : OrderEntryBase
    {
        /// <summary>
        /// Assembly-qualified type name written by <c>ManagerOrderSync</c> at edit time.
        /// Used by <c>GameLifetimeScope</c> for O(1) type resolution with no ambiguity risk.
        /// Empty on legacy assets — resolved lazily on first play-mode launch.
        /// </summary>
        [TableColumn(Header = "Manager", Editable = false)]
        public string Name;
    }
}
