using UnityEngine;
using VContainer;

public sealed partial class AxisMoveTargetInputComponentData
{
    public string Key;
    public string HorizontalAxis = "Horizontal";
    public string VerticalAxis = "Vertical";
    public string MoveTargetComponentKey = "default";
}

public sealed partial class AxisMoveTargetInputComponent
{
    private const float DeadZoneSqr = 1e-6f;

    private readonly AxisMoveTargetInputComponentData _componentData;
    [Inject] private readonly IPrefabManager _prefabManager;
    private bool _idleStopSent;

    protected override void OnUpdate()
    {
        if (GameObject == null || _prefabManager == null) return;
        if (!_prefabManager.TryGetHandle(GameObject, out var handle)) return;

        Vector2 dir = new(
            Input.GetAxisRaw(_componentData.HorizontalAxis),
            Input.GetAxisRaw(_componentData.VerticalAxis));

        string moveKey = _componentData.MoveTargetComponentKey;

        if (dir.sqrMagnitude <= DeadZoneSqr)
        {
            if (!_idleStopSent)
            {
                _prefabManager.SafeCallComponent<LinearMoveToTargetComponent>(handle, moveKey,
                    m => m.StopMove());
                _idleStopSent = true;
            }
            return;
        }

        _idleStopSent = false;

        if (dir.sqrMagnitude > 1f) dir.Normalize();

        Vector2 current = GameObject.transform.position;
        Vector2 targetPos = current + dir;

        _prefabManager.SafeCallComponent<LinearMoveToTargetComponent>(handle, moveKey,
            m => m.SetMoveTarget(targetPos));
    }
}
