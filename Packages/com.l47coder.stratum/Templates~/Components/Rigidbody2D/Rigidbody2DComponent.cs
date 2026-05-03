using Stratum;
using UnityEngine;

public sealed partial class Rigidbody2DComponentData
{
    public string Key;
    public RigidbodyType2D BodyType = RigidbodyType2D.Dynamic;
    public bool Simulated = true;
    public bool UseAutoMass;
    public float Mass = 1f;
    public float Drag;
    public float AngularDrag = 0.05f;
    public float GravityScale = 1f;
    public CollisionDetectionMode2D CollisionDetectionMode = CollisionDetectionMode2D.Discrete;
    public RigidbodySleepMode2D SleepMode = RigidbodySleepMode2D.StartAwake;
    public RigidbodyInterpolation2D Interpolation = RigidbodyInterpolation2D.None;
    public RigidbodyConstraints2D Constraints = RigidbodyConstraints2D.None;
}

public sealed partial class Rigidbody2DComponent
{
    private readonly Rigidbody2DComponentData _componentData;

    public Rigidbody2D Rigidbody2D { get; private set; }

    protected override void OnAdd()
    {
        Rigidbody2D                          = ComponentPool.Acquire<Rigidbody2D>(GameObject);
        Rigidbody2D.bodyType                 = _componentData.BodyType;
        Rigidbody2D.simulated                = _componentData.Simulated;
        Rigidbody2D.useAutoMass              = _componentData.UseAutoMass;
        Rigidbody2D.mass                     = _componentData.Mass;
        Rigidbody2D.drag                     = _componentData.Drag;
        Rigidbody2D.angularDrag              = _componentData.AngularDrag;
        Rigidbody2D.gravityScale             = _componentData.GravityScale;
        Rigidbody2D.collisionDetectionMode   = _componentData.CollisionDetectionMode;
        Rigidbody2D.sleepMode                = _componentData.SleepMode;
        Rigidbody2D.interpolation            = _componentData.Interpolation;
        Rigidbody2D.constraints              = _componentData.Constraints;
    }

    protected override void OnRemove()
    {
        ComponentPool.Release(Rigidbody2D);
        Rigidbody2D = null;
    }
}
