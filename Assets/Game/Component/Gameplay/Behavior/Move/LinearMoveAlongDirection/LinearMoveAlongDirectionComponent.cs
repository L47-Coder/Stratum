using UnityEngine;
using Stratum;

public sealed partial class LinearMoveAlongDirectionComponentData
{
    public string Key;
    public float MoveSpeed = 5f;
    public Vector2 MoveDirection;
}

public sealed partial class LinearMoveAlongDirectionComponent
{
    private const float DirEpsilonSqr = 1e-6f;

    private readonly LinearMoveAlongDirectionComponentData _componentData;
    private Vector2? _direction;

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool IsFinite(Vector2 value) => IsFinite(value.x) && IsFinite(value.y);
    private static float SanitizeMoveSpeed(float moveSpeed) => IsFinite(moveSpeed) ? Mathf.Max(0f, moveSpeed) : 0f;

    public void SetMoveDirection(Vector2 dir)
    {
        _direction = !IsFinite(dir) || dir.sqrMagnitude <= DirEpsilonSqr ? default : dir.normalized;
    }

    public void StopMove() => _direction = null;

    public void SetMoveSpeed(float moveSpeed) => _componentData.MoveSpeed = SanitizeMoveSpeed(moveSpeed);

    protected override void OnAdd()
    {
        _componentData.MoveSpeed = SanitizeMoveSpeed(_componentData.MoveSpeed);
        SetMoveDirection(_componentData.MoveDirection);
    }

    protected override void OnUpdate()
    {
        if (_direction == null || GameObject == null) return;

        Transform t = GameObject.transform;
        Vector3 p = t.position;
        Vector2 current = new(p.x, p.y);
        float step = SanitizeMoveSpeed(_componentData.MoveSpeed) * Time.deltaTime;
        Vector2 next = current + _direction.Value * step;
        t.position = new Vector3(next.x, next.y, p.z);
    }
}
