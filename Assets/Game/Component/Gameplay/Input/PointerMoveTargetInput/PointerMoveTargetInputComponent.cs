using UnityEngine;
using VContainer;

public sealed partial class PointerMoveTargetInputComponentData
{
    public string Key;
    public Camera WorldCamera;
    public int MouseButton = 0;
    public string MoveTargetComponentKey = "default";
}

public sealed partial class PointerMoveTargetInputComponent
{
    private readonly PointerMoveTargetInputComponentData _componentData;
    [Inject] private readonly IPrefabManager _prefabManager;

    protected override void OnUpdate()
    {
        if (GameObject == null || _prefabManager == null) return;
        if (!Input.GetMouseButtonDown(_componentData.MouseButton)) return;

        Camera cam = _componentData.WorldCamera != null ? _componentData.WorldCamera : Camera.main;
        if (cam == null) return;

        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        world.z = GameObject.transform.position.z;
        var targetPos = (Vector2)world;

        string moveKey = _componentData.MoveTargetComponentKey;
        if (_prefabManager.TryGetHandle(GameObject, out var handle))
        {
            _prefabManager.SafeCallComponent<LinearMoveToTargetComponent>(handle, moveKey, linearMove =>
                linearMove.SetMoveTarget(targetPos));
        }
    }
}
