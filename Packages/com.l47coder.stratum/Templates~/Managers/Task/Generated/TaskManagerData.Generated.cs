using System;
using Stratum;

[Serializable]
internal partial class TaskManagerData : BaseManagerData
{
    public override string GetKey() => Key;
}
