using Cysharp.Threading.Tasks;

namespace Stratum
{
    public interface IGameBoot
    {
        UniTask OnGameStart();
    }
}
