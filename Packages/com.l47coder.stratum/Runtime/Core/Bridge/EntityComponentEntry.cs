using System;
using System.Linq;
using UnityEngine;

namespace Stratum
{
    [Serializable]
    public sealed class EntityComponentEntry
    {
        [Field(Title = "类型键", Readonly = true)]
        public string EntryKey = string.Empty;

        [Field(Title = "初始化")] //是否在预制体生成时加入，彻底绑定预制体，会在回池时多删少增
        public bool InitOnStart = true;

        [Field(Title = "组件类型", Dropdown = nameof(GetComponentTypeOptions))]
        public string ComponentType;

        public void RefreshEntryKey()
        {
            var key = "default";
            if (Data != null)
            {
                var field = Data.GetType().GetField("Key",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public   |
                    System.Reflection.BindingFlags.NonPublic);
                var val = field?.GetValue(Data) as string;
                if (!string.IsNullOrEmpty(val)) key = val;
            }
            EntryKey = string.IsNullOrEmpty(ComponentType) ? key : $"{ComponentType}_{key}";
        }

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
