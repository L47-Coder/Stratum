using UnityEngine;
using VContainer;

public sealed partial class PlayerShootInputComponentData
{
    public string Key;
    public KeyCode ShootKey = KeyCode.J;
    public float ShootOffset = 0.5f;
}

public sealed partial class PlayerShootInputComponent
{
    private readonly PlayerShootInputComponentData _componentData;
    [Inject] private IPrefabManager _prefabManager;

    protected override void OnUpdate()
    {
        if (GameObject == null || _prefabManager == null) return;
        if (!Input.GetKey(_componentData.ShootKey)) return;

        Transform transform = GameObject.transform;
        Vector3 position = transform.position;
        Vector2 startPosition = (Vector2)position + (Vector2)transform.up * _componentData.ShootOffset;
        float angle = transform.eulerAngles.z;

        if (_prefabManager.TryGetHandle(GameObject, out var handle))
        {
            _prefabManager.SafeCallComponent<ShootComponent>(handle, "default", shootComponent =>
            {
                shootComponent.Shoot(startPosition, angle);
            });
        }
    }
}
