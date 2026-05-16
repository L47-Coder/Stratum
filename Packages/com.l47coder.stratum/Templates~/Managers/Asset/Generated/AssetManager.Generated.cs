using Stratum;
using UnityEngine.Scripting;

[Preserve]
internal partial class AssetManager : BaseManager<AssetManagerConfig, AssetManagerData>
{
    public override string AddressPath => "ManagerConfig/Asset";
}
