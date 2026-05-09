using System.Collections.Generic;
using Stratum;

public interface IMessageManager
{

}

internal sealed partial class MessageManagerData
{
    public string Key;
}

internal sealed partial class MessageManager : IMessageManager
{
    private readonly Dictionary<string, MessageManagerData> _managerDataDict = new();
}
