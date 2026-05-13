using Cysharp.Threading.Tasks;
using Stratum;
using UnityEngine;

public class GameBoot : MonoBehaviour, IGameBoot
{
    public async UniTask OnGameStart()
    {
        await UniTask.CompletedTask;
    }
}