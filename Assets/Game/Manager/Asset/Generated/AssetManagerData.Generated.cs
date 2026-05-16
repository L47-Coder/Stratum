using System;
using Stratum;

[Serializable]
internal partial class AssetManagerData : BaseManagerData
{
    public override string GetKey() => Key;
}
