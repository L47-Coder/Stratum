using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Stratum
{
    public interface IManager
    {
        UniTask SetManagerDataDict();
    }

    public abstract class BaseManager<TConfig, TData> : IManager
        where TConfig : BaseManagerConfig<TData>
        where TData : BaseManagerData
    {
        protected readonly Dictionary<string, TData> _dataDict = new();

        public virtual string AddressPath => $"ManagerConfig/{GetType().Name.Replace("Manager", "")}";

        public async UniTask SetManagerDataDict()
        {
            var config = await FrameworkLoader.LoadAsync<TConfig>(AddressPath);
            _dataDict.Clear();
            foreach (var d in config.DataList)
            {
                if (d == null || string.IsNullOrWhiteSpace(d.Key)) continue;
                if (!_dataDict.TryAdd(d.Key, d))
                    Debug.LogWarning($"[{GetType().Name}] Duplicate key '{d.Key}'.");
            }
        }
    }
}
