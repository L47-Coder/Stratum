using Stratum;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class SoRow
    {
        [Field(Title = "Name", Width = 200f)]
        public string Name;

        [Field(Title = "Address", Width = 260f)]
        public string Address;

        [Field(Hide = true)]
        public ScriptableObject Target;

        [Field(Hide = true)]
        public string AssetPath;
    }
}
