using System;
using UnityEngine;

public sealed partial class MouseInputComponentData
{
    public string Key;
}

public sealed partial class MouseInputComponent
{
    private readonly MouseInputComponentData _componentData;
    private event Action<int> MouseDown;
    private event Action<int> MouseHeld;
    private event Action<int> MouseUp;

    public void OnMouseDown(Action<int> action) => MouseDown += action;
    public void OnMouseHeld(Action<int> action) => MouseHeld += action;
    public void OnMouseUp(Action<int> action) => MouseUp += action;

    protected override void OnUpdate()
    {
        if (GameObject == null) return;

        for (int button = 0; button < 3; button++)
        {
            if (Input.GetMouseButtonDown(button)) MouseDown?.Invoke(button);
            if (Input.GetMouseButton(button)) MouseHeld?.Invoke(button);
            if (Input.GetMouseButtonUp(button)) MouseUp?.Invoke(button);
        }
    }

    protected override void OnRemove()
    {
        MouseDown = null;
        MouseHeld = null;
        MouseUp = null;
    }
}
