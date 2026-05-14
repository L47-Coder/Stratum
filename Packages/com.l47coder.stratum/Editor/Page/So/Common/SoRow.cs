using Stratum;
using UnityEngine;

namespace Stratum.Editor
{
    internal enum SoRowStatus
    {
        Ok,
        NotAddressable,
    }

    internal sealed class SoRow
    {
        [Field(Title = "Name", Width = 180f, Readonly = true)]
        public string Name;

        [Field(Title = "Address", Width = 260f, Readonly = true)]
        public string Address;

        [Field(Title = "Status", Width = 110f, Readonly = true)]
        public SoRowStatus Status;

        [Field(Hide = true)]
        public ScriptableObject Target;

        [Field(Hide = true)]
        public string AssetPath;
    }
}
