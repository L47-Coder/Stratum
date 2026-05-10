using System;
using System.Collections.Generic;
using System.Collections;
using Cysharp.Threading.Tasks;
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

    public abstract class BaseManagerData { }

    public abstract class BaseManager
    {
        protected abstract UniTask SetManagerDataDict();
        internal async UniTask InternalSetManagerDataDict() => await SetManagerDataDict();
    }

    public interface IGameBoot
    {
        UniTask OnGameStart();
    }
}
