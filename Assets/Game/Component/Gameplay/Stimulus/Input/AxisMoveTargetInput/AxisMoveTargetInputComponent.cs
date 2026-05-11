using UnityEngine;
using VContainer;

public sealed partial class AxisMoveTargetInputComponentData
{
    public string Key;
    public string HorizontalAxis = "Horizontal";
    public string VerticalAxis = "Vertical";
    public string StimulusChannel = "default";
}

public sealed partial class AxisMoveTargetInputComponent
{
    private readonly AxisMoveTargetInputComponentData _componentData;
    [Inject] private readonly IEventManager _eventManager;

    protected override void OnUpdate()
    {
        if (GameObject == null || _eventManager == null) return;

        Vector2 value = new(
            Input.GetAxisRaw(_componentData.HorizontalAxis),
            Input.GetAxisRaw(_componentData.VerticalAxis));

        _eventManager.Publish(new AxisStimulusEvent(GameObject, _componentData.StimulusChannel, value));
    }
}
