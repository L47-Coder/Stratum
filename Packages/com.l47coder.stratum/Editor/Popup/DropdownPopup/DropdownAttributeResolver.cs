using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Stratum.Editor
{
    internal static class DropdownAttributeResolver
    {
        private static readonly Dictionary<FieldInfo, string[]> Cache = new();

        public static string[] ResolveItems(FieldInfo field, string methodName)
        {
            if (field == null) return Array.Empty<string>();
            if (Cache.TryGetValue(field, out var cached)) return cached;

            string[] result = null;
            if (!string.IsNullOrEmpty(methodName))
            {
                var method = field.DeclaringType?.GetMethod(methodName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    result = method.Invoke(null, null) switch
                    {
                        string[] arr => arr,
                        List<string> list => list.ToArray(),
                        IEnumerable<string> e => e.ToArray(),
                        _ => null,
                    };
                }
            }

            return Cache[field] = result ?? Array.Empty<string>();
        }

        public static void Invalidate(FieldInfo field)
        {
            if (field != null) Cache.Remove(field);
        }

        public static void InvalidateAll() => Cache.Clear();
    }
}
