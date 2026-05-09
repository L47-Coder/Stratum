using Cysharp.Threading.Tasks;
using Stratum;
using UnityEngine;
using VContainer;

public class GameBoot : MonoBehaviour, IGameBoot
{
    [Inject] private readonly IPrefabManager _prefabManager;

    public async UniTask OnGameStart()
    {
        var handle1 = await _prefabManager.LoadPrefabAsync("Player");

        await UniTask.CompletedTask;
    }
}