using System;
using UnityEngine;

public sealed partial class ProjectileFireInputComponentData
{
    public string Key;
    public KeyCode FireKey = KeyCode.J;
}

public sealed partial class ProjectileFireInputComponent
{
    private readonly ProjectileFireInputComponentData _componentData;
    private event Action FireDown;
    private event Action FireHeld;
    private event Action FireUp;

    public void OnFireDown(Action action) => FireDown += action;
    public void OffFireDown(Action action) => FireDown -= action;
    public void OnFireHeld(Action action) => FireHeld += action;
    public void OffFireHeld(Action action) => FireHeld -= action;
    public void OnFireUp(Action action) => FireUp += action;
    public void OffFireUp(Action action) => FireUp -= action;

    protected override void OnUpdate()
    {
        if (GameObject == null) return;

        if (Input.GetKeyDown(_componentData.FireKey)) FireDown?.Invoke();
        if (Input.GetKey(_componentData.FireKey)) FireHeld?.Invoke();
        if (Input.GetKeyUp(_componentData.FireKey)) FireUp?.Invoke();
    }

    protected override void OnRemove()
    {
        FireDown = null;
        FireHeld = null;
        FireUp = null;
    }
}
