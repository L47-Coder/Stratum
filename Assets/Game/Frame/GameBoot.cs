using Cysharp.Threading.Tasks;
using Stratum;
using UnityEngine;
using VContainer;

public class GameBoot : MonoBehaviour, IGameBoot
{
    [Inject] private readonly IPrefabManager _prefabManager;
    [Inject] private readonly ITaskManager _taskManager;

    public async UniTask OnGameStart()
    {
        var handle1 = await _prefabManager.LoadPrefabAsync("Player");

        _taskManager.CreateTask()
            .Loop()
                .ActionAsync(async (token) =>
                {
                    var handle = await _prefabManager.LoadPrefabAsync("Enemy");
                    handle.GameObject.transform.position = new Vector3(Random.Range(-5f, 5f), 4.5f, 0);
                })
                .WaitTime(3000)
            .End()
            .Run();

        await UniTask.CompletedTask;
    }
}