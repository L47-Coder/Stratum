using UnityEngine;

public sealed partial class MoveComponentData
{
    public string Key;
    public float MoveSpeed = 5f;
    public float TurnSpeed = 360f;
}

public sealed partial class MoveComponent
{
    private const float ArriveEpsilon = 0.001f;

    private readonly MoveComponentData _componentData;
    private Vector2 _targetPos;
    private bool _hasTarget;

    public void SetTargetPosition(Vector2 pos)
    {
        _targetPos = pos;
        _hasTarget = true;
    }

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

        Vector2 targetDirection = toTarget.normalized;
        float targetAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg - 90f;
        float nextAngle = Mathf.MoveTowardsAngle(
            transform.eulerAngles.z,
            targetAngle,
            _componentData.TurnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, nextAngle);

        Vector2 forward = new Vector2(transform.up.x, transform.up.y).normalized;
        float step = _componentData.MoveSpeed * Time.deltaTime;
        Vector2 nextPos = currentPos + forward * step;

        if (step >= toTarget.magnitude && Vector2.Dot(forward, toTarget) > 0f)
        {
            nextPos = _targetPos;
            _hasTarget = false;
        }

        transform.position = new Vector3(nextPos.x, nextPos.y, position.z);
    }
}
