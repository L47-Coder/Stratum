using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Stratum
{
    public interface IManagerConfig
    {
        IList RawDataList { get; }
    }

    public abstract class BaseManagerConfig<TData> : ScriptableObject, IManagerConfig where TData : BaseManagerData
    {
        [SerializeField] private List<TData> _dataList = new();
        public List<TData> DataList => _dataList;
        public IList RawDataList => _dataList;
    }
}
