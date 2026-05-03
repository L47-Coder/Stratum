using System;
using UnityEngine;

namespace DevWorkbench
{
    public class ComponentOrderConfig : OrderConfigBase<ComponentOrderEntry> { }

    [Serializable]
    public class ComponentOrderEntry : OrderEntryBase
    {
        /// <summary>
        /// Assembly-qualified type name written by <c>ComponentOrderSync</c> at edit time.
        /// Used for unambiguous type resolution when multiple assemblies contain same-named components.
        /// Empty on legacy assets — resolved lazily on next sync.
        /// </summary>
        [TableColumn(Header = "Component", Editable = false)]
        public string Name;
    }
}
