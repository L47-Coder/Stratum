using System;
using System.Collections.Generic;
using System.Linq;
using VContainer;
using VContainer.Unity;

namespace Stratum
{
    public class GameLifetimeScope : LifetimeScope
    {
        private static readonly Dictionary<string, Type> _managerTypeCache = new(StringComparer.Ordinal);

        protected override void Configure(IContainerBuilder builder)
        {
            var config = FrameworkLoader.LoadSync<ManagerOrderConfig>("Frame/ManagerOrder");
            foreach (var entry in config.Entries)
            {
                var type = ResolveManagerType(entry);
                if (type == null) continue;
                builder.Register(type, Lifetime.Singleton).As<IManager>().AsImplementedInterfaces();
            }
            builder.RegisterEntryPoint<GameBootstrap>();
        }

        private static Type ResolveManagerType(ManagerOrderEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Name)) return null;
            if (_managerTypeCache.TryGetValue(entry.Name, out var cached)) return cached;

            if (!string.IsNullOrEmpty(entry.AssemblyQualifiedName))
            {
                var t = Type.GetType(entry.AssemblyQualifiedName);
                if (t != null) return _managerTypeCache[entry.Name] = t;
                UnityEngine.Debug.LogWarning($"[Stratum] Manager '{entry.Name}': AQN stale, re-open Workbench to resync.");
            }

            var asmName = typeof(IManager).Assembly.GetName().Name;
            var matches = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => Array.Exists(a.GetReferencedAssemblies(), r => string.Equals(r.Name, asmName, StringComparison.Ordinal)))
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => !t.IsAbstract && typeof(IManager).IsAssignableFrom(t) && string.Equals(t.Name, entry.Name, StringComparison.Ordinal))
                .ToList();

            if (matches.Count == 0)
            {
                UnityEngine.Debug.LogError($"[Stratum] Manager '{entry.Name}' not found in any assembly.");
                return _managerTypeCache[entry.Name] = null;
            }

            if (matches.Count > 1)
                throw new InvalidOperationException($"[Stratum] Ambiguous manager '{entry.Name}': {string.Join(", ", matches.Select(t => t.FullName))}");

            return _managerTypeCache[entry.Name] = matches[0];
        }
    }
}
