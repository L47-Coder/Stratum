using System;
using UnityEngine;
using VContainer;

public sealed partial class MoveStimulusTranslatorComponentData
{
    public string Key;
    public string AxisInputComponentKey = "default";
    public string PointerInputComponentKey = "default";
    public float AxisDeadZoneSqr = 1e-6f;
    public string MoveTargetComponentKey = "default";
}

public sealed partial class MoveStimulusTranslatorComponent
{
    private readonly MoveStimulusTranslatorComponentData _componentData;
    [Inject] private readonly IPrefabManager _prefabManager;

    private Action<Vector2> _onAxisChanged;
    private Action<Vector2> _onPointerClicked;
    private bool _idleStopSent;

    protected override void OnAdd()
    {
        _onAxisChanged = HandleAxisInput;
        _onPointerClicked = HandlePointerInput;
    }

    protected override void OnEnable()
    {
        if (_prefabManager == null) return;
        if (!_prefabManager.TryGetHandle(GameObject, out var handle)) return;

        _prefabManager.SafeCallComponent<AxisInputComponent>(handle, _componentData.AxisInputComponentKey,
            comp => comp.OnAxisChanged(_onAxisChanged));
        _prefabManager.SafeCallComponent<PointerMoveTargetInputComponent>(handle, _componentData.PointerInputComponentKey,
            comp => comp.OnPointerClicked(_onPointerClicked));
    }

    protected override void OnDisable()
    {
        if (_prefabManager == null) return;
        if (!_prefabManager.TryGetHandle(GameObject, out var handle)) return;

        _prefabManager.SafeCallComponent<AxisInputComponent>(handle, _componentData.AxisInputComponentKey,
            comp => comp.OffAxisChanged(_onAxisChanged));
        _prefabManager.SafeCallComponent<PointerMoveTargetInputComponent>(handle, _componentData.PointerInputComponentKey,
            comp => comp.OffPointerClicked(_onPointerClicked));
    }

    private void HandleAxisInput(Vector2 value)
    {
        if (_prefabManager == null) return;
        if (!_prefabManager.TryGetHandle(GameObject, out var handle)) return;

        Vector2 direction = value;
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

        Vector2 target = (Vector2)GameObject.transform.position + direction;
        _prefabManager.SafeCallComponent<LinearMoveToTargetComponent>(handle, moveKey,
            move => move.SetMoveTarget(target));
    }

    private void HandlePointerInput(Vector2 worldPosition)
    {
        if (_prefabManager == null) return;
        if (!_prefabManager.TryGetHandle(GameObject, out var handle)) return;

        _idleStopSent = false;
        _prefabManager.SafeCallComponent<LinearMoveToTargetComponent>(handle, _componentData.MoveTargetComponentKey,
            move => move.SetMoveTarget(worldPosition));
    }
}
