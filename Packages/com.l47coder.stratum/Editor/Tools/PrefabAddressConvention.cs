namespace Stratum.Editor
{
    internal static class PrefabAddressConvention
    {
        public const string AddressPrefix = "Prefab/";

        public static string AddressOf(string prefabName)
            => string.IsNullOrEmpty(prefabName) ? null : AddressPrefix + prefabName;
    }
}
