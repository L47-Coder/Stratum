using System.Threading;
using Cysharp.Threading.Tasks;

namespace Stratum
{
    public interface IAsyncInitManager
    {
        UniTask InitAsync(CancellationToken token);
    }
}
