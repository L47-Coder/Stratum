using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace Stratum
{
    public abstract class BaseManagerConfig : ScriptableObject
    {
        public abstract Type ConfigItemType { get; }
        public abstract IList GetConfigList();

        protected abstract Dictionary<string, BaseManagerData> GetManagerDataDict();
        internal Dictionary<string, BaseManagerData> ExportManagerDataDict() => GetManagerDataDict();
    }
}
