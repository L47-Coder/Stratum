using Cysharp.Threading.Tasks;

namespace Stratum
{
    public abstract class BaseManager
    {
        protected abstract UniTask SetManagerDataDict();
        internal async UniTask InternalSetManagerDataDict() => await SetManagerDataDict();
    }
}
