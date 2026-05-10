using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

public sealed partial class ProjectileSpawnComponentData
{
    public string Key;
    public string ProjectilePrefabAddress = "Bullet";
}

public sealed partial class ProjectileSpawnComponent
{
    private readonly ProjectileSpawnComponentData _componentData;
    [Inject] private readonly IPrefabManager _prefabManager;

    public void Spawn(Vector2 startPosition, float angle)
    {
        if (_prefabManager == null) return;
        SpawnAsync(startPosition, angle).Forget();
    }

    private async UniTask SpawnAsync(Vector2 startPosition, float angle)
    {
        string prefabKey = _componentData.ProjectilePrefabAddress;
        if (string.IsNullOrWhiteSpace(prefabKey)) return;

        IPrefabHandle handle = await _prefabManager.LoadPrefabAsync(prefabKey);
        Transform t = handle.GameObject.transform;
        Vector3 spawnPosition = new(startPosition.x, startPosition.y, t.position.z);
        Quaternion spawnRotation = Quaternion.Euler(0f, 0f, angle);
        t.SetPositionAndRotation(spawnPosition, spawnRotation);
        _prefabManager.SafeCallComponent<LinearMoveAlongDirectionComponent>(handle, "default", move =>
            move.SetMoveDirection((Vector2)t.up));
    }
}
