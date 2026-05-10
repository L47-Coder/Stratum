using UnityEngine;
using Stratum;

public sealed partial class LinearMoveAlongDirectionComponentData
{
    public string Key;
    public float MoveSpeed = 5f;
    public Vector2 MoveDirection = Vector2.right;
}

public sealed partial class LinearMoveAlongDirectionComponent
{
    private readonly LinearMoveAlongDirectionComponentData _componentData;
    private Vector2 _normalizedDirection;
    private bool _hasDirection;

    public void SetMoveDirection(Vector2 direction)
    {
        if (!TryNormalize(direction, out Vector2 normalized)) return;
        _normalizedDirection = normalized;
        _hasDirection = true;
    }

    protected override void OnEnable()
    {
        RefreshDirectionFromComponentData();
    }

    protected override void OnUpdate()
    {
        if (!_hasDirection || GameObject == null) return;

        Transform t = GameObject.transform;
        Vector3 position = t.position;
        Vector2 delta = _normalizedDirection * (_componentData.MoveSpeed * Time.deltaTime);
        t.position = new Vector3(position.x + delta.x, position.y + delta.y, position.z);
    }

    private void RefreshDirectionFromComponentData()
    {
        if (!TryNormalize(_componentData.MoveDirection, out Vector2 normalized)) return;
        _normalizedDirection = normalized;
        _hasDirection = true;
    }

    private static bool TryNormalize(Vector2 direction, out Vector2 normalized)
    {
        float sqrMag = direction.sqrMagnitude;
        if (sqrMag <= 0f)
        {
            normalized = default;
            return false;
        }

        normalized = direction * (1f / Mathf.Sqrt(sqrMag));
        return true;
    }
}
