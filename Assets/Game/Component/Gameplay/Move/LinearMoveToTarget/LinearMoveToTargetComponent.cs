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
    private Vector2 _targetPos;
    private bool _hasTarget;

    public void SetTargetPosition(Vector2 pos)
    {
        _targetPos = pos;
        _hasTarget = true;
    }

    public void SetMoveSpeed(float moveSpeed) => _componentData.MoveSpeed = moveSpeed;

    protected override void OnUpdate()
    {
        if (!_hasTarget || GameObject == null) return;

        Transform transform = GameObject.transform;
        Vector3 position = transform.position;
        Vector2 currentPos = new(position.x, position.y);
        Vector2 toTarget = _targetPos - currentPos;

        if (toTarget.sqrMagnitude <= ArriveEpsilon * ArriveEpsilon)
        {
            transform.position = new Vector3(_targetPos.x, _targetPos.y, position.z);
            _hasTarget = false;
            return;
        }

        float step = _componentData.MoveSpeed * Time.deltaTime;
        Vector2 nextPos = Vector2.MoveTowards(currentPos, _targetPos, step);
        transform.position = new Vector3(nextPos.x, nextPos.y, position.z);

        if ((_targetPos - nextPos).sqrMagnitude <= ArriveEpsilon * ArriveEpsilon)
            _hasTarget = false;
    }
}
