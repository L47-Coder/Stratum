using Stratum;
using UnityEngine.Scripting;

[Preserve]
internal partial class EventManager : BaseManager<EventManagerConfig, EventManagerData>
{
    public override string AddressPath => "ManagerConfig/Event";
}
