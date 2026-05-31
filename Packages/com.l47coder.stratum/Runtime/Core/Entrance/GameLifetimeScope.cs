using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VContainer;
using VContainer.Unity;

namespace Stratum
{
    public class GameLifetimeScope : LifetimeScope
    {
        private static readonly Dictionary<string, Type> _managerTypeCache = new(StringComparer.Ordinal);

        protected override void Configure(IContainerBuilder builder)
        {
            var config = FrameworkLoader.LoadSync<ManagerOrderConfig>("App/ManagerOrder");
            if (config == null)
                throw new InvalidOperationException("[Stratum] ManagerOrderConfig not found at Addressables address 'App/ManagerOrder'.");

            _managerTypeCache.Clear();
            var managerTypes = GetGameManagerTypes();
            ManagerOrderRules.ValidateManagerTypes(managerTypes);
            var managerTypesByName = managerTypes.ToDictionary(t => t.Name, t => t, StringComparer.Ordinal);
            var resolvedTypes = new List<Type>();

            foreach (var entry in config.Entries)
            {
                resolvedTypes.Add(ResolveManagerType(entry, managerTypesByName));
            }

            ManagerOrderRules.ValidateManagerTypes(resolvedTypes);
            foreach (var type in resolvedTypes)
                builder.Register(type, Lifetime.Singleton).AsImplementedInterfaces();

            builder.RegisterEntryPoint<GameBootstrap>();
        }

        private static Type ResolveManagerType(ManagerOrderEntry entry, IReadOnlyDictionary<string, Type> managerTypesByName)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Name))
                throw new InvalidOperationException("[Stratum] ManagerOrder contains an empty Manager entry.");

            if (_managerTypeCache.TryGetValue(entry.Name, out var cached)) return cached;

            if (!string.IsNullOrEmpty(entry.AssemblyQualifiedName))
            {
                var t = Type.GetType(entry.AssemblyQualifiedName);
                if (t != null)
                {
                    ValidateResolvedManagerType(entry, t);
                    return _managerTypeCache[entry.Name] = t;
                }

                UnityEngine.Debug.LogWarning($"[Stratum] Manager '{entry.Name}': AQN stale, open Tools > Stratum > Manager Order.");
            }

            if (!managerTypesByName.TryGetValue(entry.Name, out var type))
                throw new InvalidOperationException($"[Stratum] Manager '{entry.Name}' not found in assembly '{ManagerOrderConfig.ManagerAssemblyName}'. Managers must live under Assets/Game/Manager.");

            return _managerTypeCache[entry.Name] = type;
        }

        private static IReadOnlyList<Type> GetGameManagerTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => string.Equals(a.GetName().Name, ManagerOrderConfig.ManagerAssemblyName, StringComparison.Ordinal))
                .SelectMany(SafeGetTypes)
                .Where(ManagerOrderRules.IsConcreteManagerType)
                .ToList();
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }

        private static void ValidateResolvedManagerType(ManagerOrderEntry entry, Type type)
        {
            if (!ManagerOrderRules.IsConcreteManagerType(type))
                throw new InvalidOperationException($"[Stratum] Manager '{entry.Name}' resolved to '{FormatType(type)}', but it is not a concrete IManager type.");

            if (!ManagerOrderRules.IsManagerAssembly(type))
                throw new InvalidOperationException($"[Stratum] Manager '{entry.Name}' resolved to '{FormatType(type)}' in assembly '{type.Assembly.GetName().Name}', but only '{ManagerOrderConfig.ManagerAssemblyName}' managers are allowed.");

            if (!string.Equals(type.Name, entry.Name, StringComparison.Ordinal))
                throw new InvalidOperationException($"[Stratum] ManagerOrder entry '{entry.Name}' points to '{FormatType(type)}'. Open Tools > Stratum > Manager Order to refresh the asset.");
        }

        private static string FormatType(Type type)
        {
            return string.IsNullOrEmpty(type.FullName) ? type.Name : type.FullName;
        }
    }
}
