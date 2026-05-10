using UnityEngine;
using Stratum;

public sealed partial class LinearMoveToTargetComponentData
{
    public string Key;
    public float MoveSpeed = 5f;
}

public sealed partial class LinearMoveToTargetComponent
{
    private const float ArriveEpsilon = 0.001f;

    private readonly LinearMoveToTargetComponentData _componentData;
    private Vector2? _targetPos;

    public void SetMoveTarget(Vector2 pos) => _targetPos = pos;
    public void StopMove() => _targetPos = null;
    public void SetMoveSpeed(float moveSpeed) => _componentData.MoveSpeed = moveSpeed;

    protected override void OnUpdate()
    {
        if (_targetPos == null || GameObject == null) return;

        Transform t = GameObject.transform;
        Vector3 p = t.position;
        Vector2 current = new(p.x, p.y);
        Vector2 target = _targetPos.Value;

        if (Vector2.Distance(current, target) <= ArriveEpsilon)
        {
            t.position = new Vector3(target.x, target.y, p.z);
            _targetPos = null;
            return;
        }

        float step = _componentData.MoveSpeed * Time.deltaTime;
        Vector2 next = Vector2.MoveTowards(current, target, step);
        t.position = new Vector3(next.x, next.y, p.z);

        if ((target - next).sqrMagnitude <= ArriveEpsilon * ArriveEpsilon)
            _targetPos = null;
    }
}
