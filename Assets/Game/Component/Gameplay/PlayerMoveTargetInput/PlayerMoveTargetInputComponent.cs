using UnityEngine;
using VContainer;

public sealed partial class PlayerMoveTargetInputComponentData
{
    public string Key;
}

public sealed partial class PlayerMoveTargetInputComponent
{
    private readonly PlayerMoveTargetInputComponentData _componentData;
    [Inject] private readonly IPrefabManager _prefabManager;

    private Vector2 _dir;

    protected override void OnUpdate()
    {
        if (GameObject == null || _prefabManager == null) return;

        _dir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (_dir.sqrMagnitude > 1f) _dir.Normalize();

        Vector3 currentPosition = GameObject.transform.position;
        Vector2 currentPos = new(currentPosition.x, currentPosition.y);
        Vector2 targetPos = currentPos + _dir;

        if (_prefabManager.TryGetHandle(GameObject, out var handle))
        {
            _prefabManager.SafeCallComponent<MoveComponent>(handle, "default", moveComponent =>
            {
                moveComponent.SetTargetPosition(targetPos);
            });
        }
    }
}
