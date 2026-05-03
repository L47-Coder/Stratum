using System;
using System.Collections.Generic;
using System.Text;
using VContainer;
using VContainer.Unity;

namespace DevWorkbench
{
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            var config = FrameworkLoader.LoadSync<ManagerOrderConfig>("Frame/ManagerOrder");

            foreach (var entry in config.Entries)
            {
                var type = ResolveManagerType(entry);
                if (type == null) continue;

                builder.Register(type, Lifetime.Singleton).As<BaseManager>().AsImplementedInterfaces();
            }
            builder.RegisterEntryPoint<GameBootstrap>();
        }

        private static readonly Dictionary<string, Type> _managerTypeCache = new(StringComparer.Ordinal);

        private static Type ResolveManagerType(ManagerOrderEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Name)) return null;

            if (_managerTypeCache.TryGetValue(entry.Name, out var cached)) return cached;

            // Fast path: AQN was written by ManagerOrderSync at edit time — O(1), no ambiguity.
            if (!string.IsNullOrEmpty(entry.AssemblyQualifiedName))
            {
                var t = Type.GetType(entry.AssemblyQualifiedName);
                if (t != null)
                {
                    _managerTypeCache[entry.Name] = t;
                    return t;
                }

                // AQN is stale (class moved / renamed) — fall through to name scan with a warning.
                UnityEngine.Debug.LogWarning(
                    $"[DevWorkbench] Manager '{entry.Name}': stored AQN could not be resolved. " +
                    "Re-open the Dev Workbench window to resync the ManagerOrder asset.");
            }

            // Fallback: scan only assemblies that reference the DevWorkbench runtime,
            // which is a much tighter scope than all AppDomain assemblies.
            var devWorkbenchAsmName = typeof(BaseManager).Assembly.GetName().Name;
            var matches = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var refs = assembly.GetReferencedAssemblies();
                if (!Array.Exists(refs, r => string.Equals(r.Name, devWorkbenchAsmName, StringComparison.Ordinal)))
                    continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch { continue; }

                foreach (var type in types)
                {
                    if (type.IsAbstract) continue;
                    if (!typeof(BaseManager).IsAssignableFrom(type)) continue;
                    if (!string.Equals(type.Name, entry.Name, StringComparison.Ordinal)) continue;
                    matches.Add(type);
                }
            }

            if (matches.Count == 0)
            {
                UnityEngine.Debug.LogError(
                    $"[DevWorkbench] Manager type '{entry.Name}' not found in any assembly " +
                    "that references DevWorkbench. Ensure the class exists, is not abstract, " +
                    "and its assembly is loaded.");
                _managerTypeCache[entry.Name] = null;
                return null;
            }

            if (matches.Count > 1)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"[DevWorkbench] Ambiguous Manager name '{entry.Name}': {matches.Count} types found:");
                foreach (var t in matches)
                    sb.AppendLine($"  {t.FullName}  ({t.Assembly.GetName().Name})");
                sb.Append("Open the Dev Workbench window to resync the ManagerOrder asset, " +
                          "which will record the assembly-qualified name and eliminate the ambiguity.");
                throw new InvalidOperationException(sb.ToString());
            }

            _managerTypeCache[entry.Name] = matches[0];
            return matches[0];
        }
    }
}
