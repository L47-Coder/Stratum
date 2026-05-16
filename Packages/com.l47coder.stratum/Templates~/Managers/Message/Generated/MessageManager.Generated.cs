using Stratum;
using UnityEngine.Scripting;

[Preserve]
internal partial class MessageManager : BaseManager<MessageManagerConfig, MessageManagerData>
{
    public override string AddressPath => "ManagerConfig/Message";
}
