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

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool IsFinite(Vector2 value) => IsFinite(value.x) && IsFinite(value.y);
    private static float SanitizeMoveSpeed(float moveSpeed) => IsFinite(moveSpeed) ? Mathf.Max(0f, moveSpeed) : 0f;

    public void SetMoveTarget(Vector2 pos)
    {
        _targetPos = IsFinite(pos) ? pos : default;
    }
    public void StopMove() => _targetPos = null;
    public void SetMoveSpeed(float moveSpeed) => _componentData.MoveSpeed = SanitizeMoveSpeed(moveSpeed);

    protected override void OnAdd() => _componentData.MoveSpeed = SanitizeMoveSpeed(_componentData.MoveSpeed);

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

        float step = SanitizeMoveSpeed(_componentData.MoveSpeed) * Time.deltaTime;
        Vector2 next = Vector2.MoveTowards(current, target, step);
        t.position = new Vector3(next.x, next.y, p.z);

        if ((target - next).sqrMagnitude <= ArriveEpsilon * ArriveEpsilon)
            _targetPos = null;
    }
}
