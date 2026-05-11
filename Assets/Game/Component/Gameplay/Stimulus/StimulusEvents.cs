using UnityEngine;

public readonly struct AxisStimulusEvent
{
    public readonly GameObject Source;
    public readonly string Channel;
    public readonly Vector2 Value;

    public AxisStimulusEvent(GameObject source, string channel, Vector2 value)
    {
        Source = source;
        Channel = channel;
        Value = value;
    }
}

public readonly struct PointerStimulusEvent
{
    public readonly GameObject Source;
    public readonly string Channel;
    public readonly Vector2 WorldPosition;
    public readonly int Button;

    public PointerStimulusEvent(GameObject source, string channel, Vector2 worldPosition, int button)
    {
        Source = source;
        Channel = channel;
        WorldPosition = worldPosition;
        Button = button;
    }
}

public readonly struct ButtonStimulusEvent
{
    public readonly GameObject Source;
    public readonly string Channel;
    public readonly bool IsPressed;
    public readonly bool IsDown;
    public readonly bool IsUp;

    public ButtonStimulusEvent(GameObject source, string channel, bool isPressed, bool isDown, bool isUp)
    {
        Source = source;
        Channel = channel;
        IsPressed = isPressed;
        IsDown = isDown;
        IsUp = isUp;
    }
}