using Stratum;
using UnityEngine.Scripting;

[Preserve]
internal partial class TaskManager : BaseManager<TaskManagerConfig, TaskManagerData>
{
    public override string AddressPath => "ManagerConfig/Task";
}
