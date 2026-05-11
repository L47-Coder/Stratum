using UnityEngine;
using VContainer;

public sealed partial class ProjectileFireInputComponentData
{
    public string Key;
    public KeyCode FireKey = KeyCode.J;
    public string StimulusChannel = "default";
}

public sealed partial class ProjectileFireInputComponent
{
    private readonly ProjectileFireInputComponentData _componentData;
    [Inject] private readonly IEventManager _eventManager;

    protected override void OnUpdate()
    {
        if (GameObject == null || _eventManager == null) return;

        bool isPressed = Input.GetKey(_componentData.FireKey);
        bool isDown = Input.GetKeyDown(_componentData.FireKey);
        bool isUp = Input.GetKeyUp(_componentData.FireKey);
        if (!isPressed && !isDown && !isUp) return;

        _eventManager.Publish(new ButtonStimulusEvent(GameObject, _componentData.StimulusChannel, isPressed, isDown, isUp));
    }
}
