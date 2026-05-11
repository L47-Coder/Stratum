using System;
using UnityEngine;

public sealed partial class KeyboardInputComponentData
{
    public string Key;
}

public sealed partial class KeyboardInputComponent
{
    private readonly KeyboardInputComponentData _componentData;
    private event Action<KeyCode> KeyDown;
    private event Action<KeyCode> KeyHeld;
    private event Action<KeyCode> KeyUp;

    public void OnKeyDown(Action<KeyCode> action) => KeyDown += action;
    public void OnKeyHeld(Action<KeyCode> action) => KeyHeld += action;
    public void OnKeyUp(Action<KeyCode> action) => KeyUp += action;

    private static readonly KeyCode[] AllKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));

    protected override void OnUpdate()
    {
        if (GameObject == null) return;
        if (!Input.anyKey && !Input.anyKeyDown) return;

        foreach (KeyCode keyCode in AllKeyCodes)
        {
            if (Input.GetKeyDown(keyCode)) KeyDown?.Invoke(keyCode);
            if (Input.GetKey(keyCode)) KeyHeld?.Invoke(keyCode);
            if (Input.GetKeyUp(keyCode)) KeyUp?.Invoke(keyCode);
        }
    }

    protected override void OnRemove()
    {
        KeyDown = null;
        KeyHeld = null;
        KeyUp = null;
    }
}
