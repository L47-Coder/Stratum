using System;
using UnityEngine;
using VContainer;

public sealed partial class StimulusToBehaviorTranslatorComponentData
{
    public string Key;
    public string AxisStimulusChannel = "default";
    public string PointerStimulusChannel = "default";
    public string ButtonStimulusChannel = "default";
    public float AxisDeadZoneSqr = 1e-6f;
    public string MoveTargetComponentKey = "default";
    public string ProjectileSpawnComponentKey = "default";
    public float FireOffset = 0.5f;
}

public sealed partial class StimulusToBehaviorTranslatorComponent
{
    private readonly StimulusToBehaviorTranslatorComponentData _componentData;
    [Inject] private readonly IEventManager _eventManager;
    [Inject] private readonly IPrefabManager _prefabManager;

    private IEventHandle _axisHandle;
    private IEventHandle _pointerHandle;
    private IEventHandle _buttonHandle;
    private bool _idleStopSent;

    protected override void OnEnable()
    {
        if (_eventManager == null) return;

        UnsubscribeStimuli();
        _axisHandle = _eventManager.Subscribe<AxisStimulusEvent>(HandleAxisStimulus);
        _pointerHandle = _eventManager.Subscribe<PointerStimulusEvent>(HandlePointerStimulus);
        _buttonHandle = _eventManager.Subscribe<ButtonStimulusEvent>(HandleButtonStimulus);
    }

    protected override void OnDisable() => UnsubscribeStimuli();

    private void HandleAxisStimulus(AxisStimulusEvent stimulus)
    {
        if (!IsSelfStimulus(stimulus.Source)) return;
        if (!IsChannel(stimulus.Channel, _componentData.AxisStimulusChannel)) return;
        if (_prefabManager == null) return;
        if (!_prefabManager.TryGetHandle(GameObject, out var handle)) return;

        Vector2 direction = stimulus.Value;
        string moveKey = _componentData.MoveTargetComponentKey;

        if (direction.sqrMagnitude <= Math.Max(0f, _componentData.AxisDeadZoneSqr))
        {
            if (!_idleStopSent)
            {
                _prefabManager.SafeCallComponent<LinearMoveToTargetComponent>(handle, moveKey,
                    move => move.StopMove());
                _idleStopSent = true;
            }
            return;
        }

        _idleStopSent = false;

        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        Vector2 current = GameObject.transform.position;
        Vector2 targetPosition = current + direction;
        _prefabManager.SafeCallComponent<LinearMoveToTargetComponent>(handle, moveKey,
            move => move.SetMoveTarget(targetPosition));
    }

    private void HandlePointerStimulus(PointerStimulusEvent stimulus)
    {
        if (!IsSelfStimulus(stimulus.Source)) return;
        if (!IsChannel(stimulus.Channel, _componentData.PointerStimulusChannel)) return;
        if (_prefabManager == null) return;
        if (!_prefabManager.TryGetHandle(GameObject, out var handle)) return;

        _idleStopSent = false;
        string moveKey = _componentData.MoveTargetComponentKey;
        _prefabManager.SafeCallComponent<LinearMoveToTargetComponent>(handle, moveKey,
            move => move.SetMoveTarget(stimulus.WorldPosition));
    }

    private void HandleButtonStimulus(ButtonStimulusEvent stimulus)
    {
        if (!IsSelfStimulus(stimulus.Source)) return;
        if (!IsChannel(stimulus.Channel, _componentData.ButtonStimulusChannel)) return;
        if (!stimulus.IsPressed) return;
        if (_prefabManager == null) return;
        if (!_prefabManager.TryGetHandle(GameObject, out var handle)) return;

        Transform transform = GameObject.transform;
        Vector3 position = transform.position;
        Vector2 startPosition = (Vector2)position + (Vector2)transform.up * _componentData.FireOffset;
        float angle = transform.eulerAngles.z;

        string spawnKey = _componentData.ProjectileSpawnComponentKey;
        _prefabManager.SafeCallComponent<ProjectileSpawnComponent>(handle, spawnKey,
            projectileSpawn => projectileSpawn.Spawn(startPosition, angle));
    }

    private bool IsSelfStimulus(GameObject source) => source != null && GameObject != null && source == GameObject;

    private static bool IsChannel(string actual, string expected)
    {
        if (string.IsNullOrWhiteSpace(expected)) return true;
        return string.Equals(actual, expected, StringComparison.Ordinal);
    }

    private void UnsubscribeStimuli()
    {
        if (_eventManager == null) return;

        _eventManager.Unsubscribe(_axisHandle);
        _eventManager.Unsubscribe(_pointerHandle);
        _eventManager.Unsubscribe(_buttonHandle);

        _axisHandle = null;
        _pointerHandle = null;
        _buttonHandle = null;
    }
}