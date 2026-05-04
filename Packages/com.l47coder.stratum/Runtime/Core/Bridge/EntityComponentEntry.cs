using System;
using System.Linq;
using UnityEngine;

namespace Stratum
{
    [Serializable]
    public sealed class EntityComponentEntry
    {
        [Field(Title = "初始化")]
        public bool InitOnStart = true;

        [Field(Title = "组件类型", Dropdown = nameof(GetComponentTypeOptions))]
        public string ComponentType;

        [Field(Title = "条目")]
        public string EntryKey = "default";

        [Field(Hide = true)]
        [SerializeReference]
        public BaseComponentData Data;

        private static string[] _componentTypeOptionsCache;

        private static string[] GetComponentTypeOptions()
        {
            if (_componentTypeOptionsCache != null) return _componentTypeOptionsCache;

            var baseType = typeof(BaseComponentData);
            _componentTypeOptionsCache = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t) && t != baseType)
                .Select(t => t.Name.EndsWith("Data", StringComparison.Ordinal) ? t.Name[..^4] : t.Name)
                .OrderBy(s => s)
                .ToArray();
            return _componentTypeOptionsCache;
        }
    }
}
