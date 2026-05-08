using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Stratum;
using UnityEngine;
using VContainer;

public class GameBoot : MonoBehaviour, IGameBoot
{
    [Inject] private readonly IPrefabManager _prefabManager;

    public async UniTask OnGameStart()
    {
        await _prefabManager.LoadPrefabAsync("Player");

        await UniTask.CompletedTask;
    }
}