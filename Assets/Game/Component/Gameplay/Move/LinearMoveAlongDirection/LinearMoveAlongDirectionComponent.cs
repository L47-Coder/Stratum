using UnityEngine;
using Stratum;

public sealed partial class LinearMoveAlongDirectionComponentData
{
    public string Key;
    public float MoveSpeed = 5f;
}

public sealed partial class LinearMoveAlongDirectionComponent
{
    private const float DirEpsilonSqr = 1e-6f;

    private readonly LinearMoveAlongDirectionComponentData _componentData;
    private Vector2? _direction;

    public void SetMoveDirection(Vector2 dir) => _direction = dir.sqrMagnitude <= DirEpsilonSqr ? null : dir.normalized;

    public void StopMove() => _direction = null;

    public void SetMoveSpeed(float moveSpeed) => _componentData.MoveSpeed = moveSpeed;

    protected override void OnUpdate()
    {
        if (_direction == null || GameObject == null) return;

        Transform t = GameObject.transform;
        Vector3 p = t.position;
        Vector2 current = new(p.x, p.y);
        float step = _componentData.MoveSpeed * Time.deltaTime;
        Vector2 next = current + _direction.Value * step;
        t.position = new Vector3(next.x, next.y, p.z);
    }
}
