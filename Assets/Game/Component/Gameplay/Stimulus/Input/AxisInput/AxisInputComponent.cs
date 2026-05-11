using System;
using UnityEngine;

public sealed partial class AxisInputComponentData
{
    public string Key;
    public string HorizontalAxis = "Horizontal";
    public string VerticalAxis = "Vertical";
}

public sealed partial class AxisInputComponent
{
    private readonly AxisInputComponentData _componentData;
    private event Action<Vector2> AxisChanged;

    public void OnAxisChanged(Action<Vector2> action) => AxisChanged += action;
    public void OffAxisChanged(Action<Vector2> action) => AxisChanged -= action;

    protected override void OnUpdate()
    {
        if (GameObject == null) return;

        var axis = new Vector2(Input.GetAxisRaw(_componentData.HorizontalAxis),Input.GetAxisRaw(_componentData.VerticalAxis));
        if (axis == default) return;
        AxisChanged?.Invoke(axis);
    }

    protected override void OnRemove() => AxisChanged = null;
}
