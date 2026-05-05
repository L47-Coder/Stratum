using Stratum;

public sealed partial class AComponentData
{
    public string Key;
}

public sealed partial class AComponent
{
    private readonly AComponentData _componentData;
}
