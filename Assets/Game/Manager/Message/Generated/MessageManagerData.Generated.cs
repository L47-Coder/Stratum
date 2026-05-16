using System;
using Stratum;

[Serializable]
internal partial class MessageManagerData : BaseManagerData
{
    public override string GetKey() => Key;
}
