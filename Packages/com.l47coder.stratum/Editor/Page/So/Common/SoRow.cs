using Stratum;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class SoRow
    {
        [Field(Title = "Name")]
        public string Name;

        [Field(Hide = true)]
        public ScriptableObject Target;

        [Field(Hide = true)]
        public string AssetPath;
    }
}
