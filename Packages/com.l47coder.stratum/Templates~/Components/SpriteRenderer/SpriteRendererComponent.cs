using System.Linq;
using Stratum;
using UnityEngine;

public sealed partial class SpriteRendererComponentData
{
    public string Key;

    public Sprite Sprite;

    public Color Color = Color.white;

    public bool FlipX;

    public bool FlipY;

    [Dropdown(nameof(GetSortingLayers))]
    public string SortingLayerName = "Default";

    public int SortingOrder;

    private static string[] GetSortingLayers() =>
        SortingLayer.layers.Select(l => l.name).ToArray();
}

public sealed partial class SpriteRendererComponent
{
    private readonly SpriteRendererComponentData _componentData;

    public SpriteRenderer SpriteRenderer { get; private set; }

    protected override void OnAdd()
    {
        SpriteRenderer = ComponentPool.Acquire<SpriteRenderer>(GameObject);
        SpriteRenderer.sprite           = _componentData.Sprite;
        SpriteRenderer.color            = _componentData.Color;
        SpriteRenderer.flipX            = _componentData.FlipX;
        SpriteRenderer.flipY            = _componentData.FlipY;
        SpriteRenderer.sortingLayerName = _componentData.SortingLayerName;
        SpriteRenderer.sortingOrder     = _componentData.SortingOrder;
    }

    protected override void OnRemove()
    {
        ComponentPool.Release(SpriteRenderer);
        SpriteRenderer = null;
    }
}
