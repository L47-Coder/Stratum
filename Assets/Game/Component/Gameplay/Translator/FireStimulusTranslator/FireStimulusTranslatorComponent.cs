using System;
using UnityEngine;
using VContainer;

public sealed partial class FireStimulusTranslatorComponentData
{
    public string Key;
    public string FireInputComponentKey = "default";
    public string ProjectileSpawnComponentKey = "default";
    public float FireOffset = 0.5f;
}

public sealed partial class FireStimulusTranslatorComponent
{
    private readonly FireStimulusTranslatorComponentData _componentData;
    [Inject] private readonly IPrefabManager _prefabManager;

    private Action _onFireDown;

    protected override void OnAdd() => _onFireDown = HandleFireDown;

    protected override void OnEnable()
    {
        if (_prefabManager == null) return;
        if (!_prefabManager.TryGetHandle(GameObject, out var handle)) return;

        _prefabManager.SafeCallComponent<ProjectileFireInputComponent>(handle, _componentData.FireInputComponentKey,
            comp => comp.OnFireDown(_onFireDown));
    }

    protected override void OnDisable()
    {
        if (_prefabManager == null) return;
        if (!_prefabManager.TryGetHandle(GameObject, out var handle)) return;

        _prefabManager.SafeCallComponent<ProjectileFireInputComponent>(handle, _componentData.FireInputComponentKey,
            comp => comp.OffFireDown(_onFireDown));
    }

    private void HandleFireDown()
    {
        if (_prefabManager == null) return;
        if (!_prefabManager.TryGetHandle(GameObject, out var handle)) return;

        Transform t = GameObject.transform;
        Vector2 startPosition = (Vector2)t.position + (Vector2)t.up * _componentData.FireOffset;
        float angle = t.eulerAngles.z;

        _prefabManager.SafeCallComponent<ProjectileSpawnComponent>(handle, _componentData.ProjectileSpawnComponentKey,
            spawn => spawn.Spawn(startPosition, angle));
    }
}
