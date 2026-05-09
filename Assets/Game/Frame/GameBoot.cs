using Cysharp.Threading.Tasks;
using Stratum;
using UnityEngine;
using VContainer;

public class GameBoot : MonoBehaviour, IGameBoot
{
    [Inject] private readonly IPrefabManager _prefabManager;
    [Inject] private readonly ILayer2DManager _layer2DManager;

    public async UniTask OnGameStart()
    {
        var handle1 = await _prefabManager.LoadPrefabAsync("Player");

        await UniTask.CompletedTask;
    }
}