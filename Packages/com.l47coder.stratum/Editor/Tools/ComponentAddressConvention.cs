using System;

namespace Stratum.Editor
{
    internal static class ComponentAddressConvention
    {
        public const string AddressPrefix = "ComponentConfig/";
        private const string ConfigTypeSuffix = "ComponentConfig";

        public static string AddressOf(string componentName)
            => string.IsNullOrEmpty(componentName) ? null : AddressPrefix + componentName;

        public static string ComponentNameOf(Type configType)
        {
            var name = configType?.Name;
            if (string.IsNullOrEmpty(name) || !name.EndsWith(ConfigTypeSuffix, StringComparison.Ordinal))
                return null;
            var componentName = name[..^ConfigTypeSuffix.Length];
            return string.IsNullOrEmpty(componentName) ? null : componentName;
        }

        public static string AddressOfConfigType(Type configType)
            => AddressOf(ComponentNameOf(configType));
    }
}
