using UnityEngine;
using VContainer;

public sealed partial class PointerMoveTargetInputComponentData
{
    public string Key;
    public Camera WorldCamera;
    public int MouseButton = 0;
    public string StimulusChannel = "default";
}

public sealed partial class PointerMoveTargetInputComponent
{
    private readonly PointerMoveTargetInputComponentData _componentData;
    [Inject] private readonly IEventManager _eventManager;

    protected override void OnUpdate()
    {
        if (GameObject == null || _eventManager == null) return;
        if (!Input.GetMouseButtonDown(_componentData.MouseButton)) return;

        Camera cam = _componentData.WorldCamera != null ? _componentData.WorldCamera : Camera.main;
        if (cam == null) return;

        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        world.z = GameObject.transform.position.z;
        var worldPosition = (Vector2)world;

        _eventManager.Publish(new PointerStimulusEvent(GameObject, _componentData.StimulusChannel, worldPosition, _componentData.MouseButton));
    }
}
