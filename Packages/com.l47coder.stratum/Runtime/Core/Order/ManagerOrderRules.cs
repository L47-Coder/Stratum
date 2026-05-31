using System;
using System.Collections.Generic;
using System.Linq;

namespace Stratum
{
    public static class ManagerOrderRules
    {
        public static bool IsConcreteManagerType(Type type)
        {
            return type != null
                && !type.IsAbstract
                && !type.IsInterface
                && typeof(IManager).IsAssignableFrom(type);
        }

        public static bool IsManagerAssembly(Type type)
        {
            return type != null
                && string.Equals(type.Assembly.GetName().Name, ManagerOrderConfig.ManagerAssemblyName, StringComparison.Ordinal);
        }

        public static IReadOnlyList<Type> GetManagerContractTypes(Type managerType)
        {
            if (managerType == null) return Array.Empty<Type>();

            return managerType.GetInterfaces()
                .Where(t => t != typeof(IManager) && t != typeof(IAsyncInitManager))
                .OrderBy(t => t.FullName, StringComparer.Ordinal)
                .ToArray();
        }

        public static void ValidateManagerTypes(IEnumerable<Type> managerTypes)
        {
            if (managerTypes == null) return;

            var types = managerTypes
                .Where(t => t != null)
                .OrderBy(t => t.FullName, StringComparer.Ordinal)
                .ToList();

            var errors = new List<string>();

            var duplicateNames = types
                .GroupBy(t => t.Name, StringComparer.Ordinal)
                .Where(g => g.Count() > 1);
            foreach (var group in duplicateNames)
            {
                errors.Add($"Duplicate manager name '{group.Key}': {FormatTypes(group)}. Manager names must be unique because ManagerOrder stores entries by name.");
            }

            var duplicateContracts = types
                .SelectMany(t => GetManagerContractTypes(t).Select(contract => new { Contract = contract, Manager = t }))
                .GroupBy(x => x.Contract)
                .Where(g => g.Select(x => x.Manager).Distinct().Count() > 1);
            foreach (var group in duplicateContracts)
            {
                var managers = group.Select(x => x.Manager).Distinct().OrderBy(t => t.FullName, StringComparer.Ordinal);
                errors.Add($"Duplicate manager contract '{FormatType(group.Key)}': {FormatTypes(managers)}. Only one Manager may implement the same business interface.");
            }

            if (errors.Count == 0) return;

            throw new InvalidOperationException("[Stratum] Invalid Manager configuration:\n" + string.Join("\n", errors));
        }

        private static string FormatTypes(IEnumerable<Type> types)
        {
            return string.Join(", ", types.Select(FormatType));
        }

        private static string FormatType(Type type)
        {
            return string.IsNullOrEmpty(type.FullName) ? type.Name : type.FullName;
        }
    }
}
