using System.Collections.Generic;
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
    private readonly List<ITaskHandle> _spawnTasks = new();
    private float _lastSpawnTime = float.NegativeInfinity;

    public bool Spawn(Vector2 startPosition, float angle)
    {
        if (_prefabManager == null || _taskManager == null) return false;

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

        bool completed = false;
        ITaskHandle taskHandle = null;
        taskHandle = _taskManager.CreateTask().ActionAsync(async token =>
        {
            try
            {
                if (token.IsCancellationRequested) return;

                IPrefabHandle handle = await _prefabManager.LoadPrefabAsync(prefabKey);
                if (token.IsCancellationRequested)
                {
                    await _prefabManager.ReleasePrefabAsync(handle);
                    return;
                }

                Transform t = handle.GameObject.transform;
                Vector3 spawnPosition = new(startPosition.x, startPosition.y, t.position.z);
                Quaternion spawnRotation = Quaternion.Euler(0f, 0f, angle);
                t.SetPositionAndRotation(spawnPosition, spawnRotation);
                _prefabManager.SafeCallComponent<LinearMoveAlongDirectionComponent>(handle, "default", move => move.SetMoveDirection((Vector2)t.up));
            }
            finally
            {
                completed = true;
                if (taskHandle != null)
                    _spawnTasks.Remove(taskHandle);
            }
        }).Run();

        if (!completed)
            _spawnTasks.Add(taskHandle);

        return true;
    }

    protected override void OnRemove()
    {
        if (_taskManager != null)
        {
            for (int i = _spawnTasks.Count - 1; i >= 0; i--)
                _taskManager.StopTask(_spawnTasks[i]);
        }

        _spawnTasks.Clear();
    }
}
