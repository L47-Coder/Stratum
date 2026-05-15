using Stratum;

internal sealed partial class AssetManagerData
{
    [Field(Readonly = true, Width = 300)]
    public string Key;

    [Field(Readonly = true)]
    public string AssetAddress;
}
