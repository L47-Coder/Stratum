using System;
using UnityEngine;

public sealed partial class PointerMoveTargetInputComponentData
{
    public string Key;
    public Camera WorldCamera;
    public int MouseButton = 0;
}

public sealed partial class PointerMoveTargetInputComponent
{
    private readonly PointerMoveTargetInputComponentData _componentData;
    private event Action<Vector2> PointerClicked;

    public void OnPointerClicked(Action<Vector2> action) => PointerClicked += action;
    public void OffPointerClicked(Action<Vector2> action) => PointerClicked -= action;

    protected override void OnUpdate()
    {
        if (GameObject == null) return;
        if (!Input.GetMouseButtonDown(_componentData.MouseButton)) return;

        Camera cam = _componentData.WorldCamera != null ? _componentData.WorldCamera : Camera.main;
        if (cam == null) return;

        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        world.z = GameObject.transform.position.z;
        PointerClicked?.Invoke((Vector2)world);
    }

    protected override void OnRemove() => PointerClicked = null;
}
