using System;
using Stratum;

[Serializable]
internal partial class EventManagerData : BaseManagerData
{
    public override string GetKey() => Key;
}
