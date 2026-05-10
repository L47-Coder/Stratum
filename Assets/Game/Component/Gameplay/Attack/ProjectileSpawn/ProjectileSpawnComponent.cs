using System;
using UnityEngine;
using VContainer;

public sealed partial class ProjectileSpawnComponentData
{
    public string Key;
    public string ProjectilePrefabAddress = "Bullet";
    public float SpawnsPerSecond = 5f;
}

public sealed partial class ProjectileSpawnComponent
{
    private readonly ProjectileSpawnComponentData _componentData;
    [Inject] private readonly IPrefabManager _prefabManager;
    [Inject] private readonly ITaskManager _taskManager;
    private float _lastSpawnTime = float.NegativeInfinity;

    public bool Spawn(Vector2 startPosition, float angle)
    {
        if (_prefabManager == null) return false;

        string prefabKey = _componentData.ProjectilePrefabAddress;
        if (string.IsNullOrWhiteSpace(prefabKey)) return false;

        float rate = _componentData.SpawnsPerSecond;
        if (rate > 0f)
        {
            float minInterval = 1f / rate;
            if (Time.time - _lastSpawnTime < minInterval)
                return false;
        }

        _lastSpawnTime = Time.time;

        _taskManager.CreateTask().ActionAsync(async (token) =>
        {
            IPrefabHandle handle = await _prefabManager.LoadPrefabAsync(_componentData.ProjectilePrefabAddress);
            Transform t = handle.GameObject.transform;
            Vector3 spawnPosition = new(startPosition.x, startPosition.y, t.position.z);
            Quaternion spawnRotation = Quaternion.Euler(0f, 0f, angle);
            t.SetPositionAndRotation(spawnPosition, spawnRotation);
            _prefabManager.SafeCallComponent<LinearMoveAlongDirectionComponent>(handle, "default", move => move.SetMoveDirection((Vector2)t.up));
        }).Run();

        return true;
    }
}
