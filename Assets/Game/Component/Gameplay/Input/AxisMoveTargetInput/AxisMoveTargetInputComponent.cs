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
    private readonly AxisMoveTargetInputComponentData _componentData;
    [Inject] private readonly IPrefabManager _prefabManager;

    protected override void OnUpdate()
    {
        if (GameObject == null || _prefabManager == null) return;

        Vector2 dir = new(
            Input.GetAxisRaw(_componentData.HorizontalAxis),
            Input.GetAxisRaw(_componentData.VerticalAxis));
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        Vector3 currentPosition = GameObject.transform.position;
        Vector2 currentPos = new(currentPosition.x, currentPosition.y);
        Vector2 targetPos = currentPos + dir;

        string moveKey = _componentData.MoveTargetComponentKey;
        if (_prefabManager.TryGetHandle(GameObject, out var handle))
        {
            _prefabManager.SafeCallComponent<LinearMoveToTargetComponent>(handle, moveKey, linearMove =>
                linearMove.SetTargetPosition(targetPos));
        }
    }
}
