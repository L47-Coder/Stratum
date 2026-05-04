using System;

namespace Stratum.Editor
{
    internal static class ManagerAddressConvention
    {
        public const string AddressPrefix = "ManagerConfig/";
        private const string ConfigTypeSuffix = "ManagerConfig";

        public static string AddressOf(string managerName)
            => string.IsNullOrEmpty(managerName) ? null : AddressPrefix + managerName;

        public static string ManagerNameOf(Type configType)
        {
            var name = configType?.Name;
            if (string.IsNullOrEmpty(name) || !name.EndsWith(ConfigTypeSuffix, StringComparison.Ordinal))
                return null;
            var managerName = name[..^ConfigTypeSuffix.Length];
            return string.IsNullOrEmpty(managerName) ? null : managerName;
        }

        public static string AddressOfConfigType(Type configType)
            => AddressOf(ManagerNameOf(configType));
    }
}
