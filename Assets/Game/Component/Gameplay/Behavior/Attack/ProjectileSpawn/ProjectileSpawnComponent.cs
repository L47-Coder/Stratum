using System.Collections.Generic;
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
    [Inject] private readonly ITaskManager _taskManager;
    private readonly List<ITaskHandle> _spawnTasks = new();

    public bool Spawn(Vector2 startPosition, float angle)
    {
        if (_prefabManager == null || _taskManager == null) return false;

        string prefabKey = _componentData.ProjectilePrefabAddress;
        if (string.IsNullOrWhiteSpace(prefabKey)) return false;

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
                t.SetPositionAndRotation(
                    new Vector3(startPosition.x, startPosition.y, t.position.z),
                    Quaternion.Euler(0f, 0f, angle));
            }
            finally
            {
                _spawnTasks.Remove(taskHandle);
            }
        }).Run();

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
