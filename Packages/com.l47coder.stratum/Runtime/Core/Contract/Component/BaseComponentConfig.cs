using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace Stratum
{
    public abstract class BaseComponentConfig : ScriptableObject
    {
        public abstract Type ConfigItemType { get; }
        public abstract IList GetConfigList();

        protected abstract Dictionary<string, BaseComponentData> GetComponentDataDict();
        internal Dictionary<string, BaseComponentData> ExportComponentDataDict() => GetComponentDataDict();
    }
}
