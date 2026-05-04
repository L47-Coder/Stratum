using System;
using System.Linq;
using UnityEngine;

namespace Stratum
{
    [Serializable]
    public sealed class EntityComponentEntry
    {
        [TableColumn(Title = "初始化")]
        public bool InitOnStart = true;

        [Dropdown(nameof(GetComponentTypeOptions))]
        [TableColumn(Title = "组件类型")]
        public string ComponentType;

        [TableColumn(Title = "条目")]
        public string EntryKey = "default";

        [TableColumn(Hide = true)]
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
