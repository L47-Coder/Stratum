using VContainer;

public sealed partial class LifetimeComponentData
{
    public string Key;
    public int LifeTime = 5000;
}

public sealed partial class LifetimeComponent
{
    private readonly LifetimeComponentData _componentData;
    [Inject] private readonly IPrefabManager _prefabManager;
    [Inject] private readonly ITaskManager _taskManager;
    private ITaskHandle _taskHandle;

    protected override void OnAdd()
    {
        if (_taskManager == null || _prefabManager == null || GameObject == null) return;

        _taskHandle = _taskManager.CreateTask()
            .WaitTime(_componentData.LifeTime)
            .ActionAsync(async token =>
            {
                if (token.IsCancellationRequested || GameObject == null) return;
                if (_prefabManager.TryGetHandle(GameObject, out var handle))
                    await _prefabManager.ReleasePrefabAsync(handle);
            })
            .Run();
    }

    protected override void OnRemove()
    {
        _taskManager?.StopTask(_taskHandle);
        _taskHandle = null;
    }
}
