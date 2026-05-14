using System;
using System.IO;

namespace Stratum.Editor
{
    internal static class SoAddressConvention
    {
        public const string AddressPrefix = "SO/";
        public const string GroupName = "ScriptableObjects";

        public static string AddressOf(Type soType, string assetName)
        {
            if (soType == null || string.IsNullOrEmpty(assetName)) return null;
            return $"{AddressPrefix}{soType.Name}/{assetName}";
        }

        public static string AddressOfAsset(Type soType, string assetPath)
        {
            if (soType == null || string.IsNullOrEmpty(assetPath)) return null;
            return AddressOf(soType, Path.GetFileNameWithoutExtension(assetPath));
        }
    }
}
