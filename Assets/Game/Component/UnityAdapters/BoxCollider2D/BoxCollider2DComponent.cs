using Stratum;
using UnityEngine;

public sealed partial class BoxCollider2DComponentData
{
    public string Key;
    public bool IsTrigger;
    public Vector2 Offset;
    public Vector2 Size = Vector2.one;
    public float EdgeRadius;
    public bool UsedByEffector;
    public bool UsedByComposite;
}

public sealed partial class BoxCollider2DComponent
{
    private readonly BoxCollider2DComponentData _componentData;

    public BoxCollider2D BoxCollider2D { get; private set; }

    protected override void OnAdd()
    {
        BoxCollider2D = ComponentPool.Acquire<BoxCollider2D>(GameObject);
        BoxCollider2D.isTrigger = _componentData.IsTrigger;
        BoxCollider2D.offset = _componentData.Offset;
        BoxCollider2D.size = _componentData.Size;
        BoxCollider2D.edgeRadius = _componentData.EdgeRadius;
        BoxCollider2D.usedByEffector = _componentData.UsedByEffector;
        BoxCollider2D.usedByComposite = _componentData.UsedByComposite;
    }

    protected override void OnRemove()
    {
        ComponentPool.Release(BoxCollider2D);
        BoxCollider2D = null;
    }
}
