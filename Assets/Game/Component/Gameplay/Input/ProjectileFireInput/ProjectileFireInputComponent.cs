using UnityEngine;
using VContainer;

public sealed partial class ProjectileFireInputComponentData
{
    public string Key;
    public KeyCode FireKey = KeyCode.J;
    public float FireOffset = 0.5f;
    public string ProjectileSpawnComponentKey = "default";
}

public sealed partial class ProjectileFireInputComponent
{
    private readonly ProjectileFireInputComponentData _componentData;
    [Inject] private readonly IPrefabManager _prefabManager;

    protected override void OnUpdate()
    {
        if (GameObject == null || _prefabManager == null) return;
        if (!Input.GetKey(_componentData.FireKey)) return;

        Transform transform = GameObject.transform;
        Vector3 position = transform.position;
        Vector2 startPosition = (Vector2)position + (Vector2)transform.up * _componentData.FireOffset;
        float angle = transform.eulerAngles.z;

        string spawnKey = _componentData.ProjectileSpawnComponentKey;
        if (_prefabManager.TryGetHandle(GameObject, out var handle))
        {
            _prefabManager.SafeCallComponent<ProjectileSpawnComponent>(handle, spawnKey, projectileSpawn =>
                projectileSpawn.Spawn(startPosition, angle));
        }
    }
}
