using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

public sealed partial class ShootComponentData
{
    public string Key;
    public string BulletPrefabAddress = "Bullet";
}

public sealed partial class ShootComponent
{
    private readonly ShootComponentData _componentData;
    [Inject] private IPrefabManager _prefabManager;

    public bool Shoot(Vector2 startPosition, float angle)
    {
        if (_prefabManager == null) return false;

        ShootAsync(startPosition, angle).Forget();
        return true;
    }

    private async UniTask ShootAsync(Vector2 startPosition, float angle)
    {
        try
        {
            string bulletPrefabKey = GetBulletPrefabKey();
            if (string.IsNullOrWhiteSpace(bulletPrefabKey)) return;

            IPrefabHandle bulletHandle = await _prefabManager.LoadPrefabAsync(bulletPrefabKey);
            Transform bulletTransform = bulletHandle.GameObject.transform;
            Vector3 spawnPosition = new(startPosition.x, startPosition.y, bulletTransform.position.z);
            Quaternion spawnRotation = Quaternion.Euler(0f, 0f, angle);
            bulletTransform.SetPositionAndRotation(spawnPosition, spawnRotation);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private string GetBulletPrefabKey()
    {
        if (string.IsNullOrWhiteSpace(_componentData.BulletPrefabAddress)) return "Bullet";

        string key = _componentData.BulletPrefabAddress.Trim();
        const string prefabAddressPrefix = "Prefab/";
        return key.StartsWith(prefabAddressPrefix) ? key[prefabAddressPrefix.Length..] : key;
    }
}
