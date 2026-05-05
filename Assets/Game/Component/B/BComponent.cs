using Stratum;

public sealed partial class BComponentData
{
    [Field(Title = "KKKKK")]
    public string Key;

    [Field(Dropdown = nameof(Get))]
    public string a;

    public static string[] Get()
    {
        return new string[] { "111", "222", "333" };
    }
}

public sealed partial class BComponent
{
    private readonly BComponentData _componentData;
}
