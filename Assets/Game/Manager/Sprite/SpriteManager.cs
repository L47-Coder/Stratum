using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Stratum;

public interface ISpriteHandle
{
    public Sprite Result { get; }
}

public interface ISpriteManager
{
    public UniTask<ISpriteHandle> LoadSpriteAsync(string key);
    public UniTask Release(ISpriteHandle handle);
    public UniTask UnloadAll();
}

internal sealed class SpriteHandle : ISpriteHandle
{
    public string Key { get; }
    public Sprite Result { get; }

    public SpriteHandle(string key, Sprite result)
    {
        Result = result;
        Key = key;
    }
}

internal sealed partial class SpriteManagerData
{
    [Field(Readonly = true, Width = 300)]
    public string Key;

    [Field(Readonly = true)]
    public string SpriteAddress;
}

internal sealed partial class SpriteManager : ISpriteManager
{
    private readonly Dictionary<string, SpriteManagerData> _managerDataDict = new();
    private readonly Dictionary<string, SpriteCache> _spriteCaches = new(StringComparer.Ordinal);

    private readonly IAssetManager _assetManager;

    public SpriteManager(IAssetManager assetManager) => _assetManager = assetManager;

    private sealed class SpriteCache
    {
        public readonly UniTaskCompletionSource<IAssetHandle<Sprite>> AssetCompletion = new();
        public readonly HashSet<ISpriteHandle> SpriteHandles = new();
        public IAssetHandle<Sprite> AssetHandle;
    }

    public async UniTask<ISpriteHandle> LoadSpriteAsync(string key)
    {
        if (string.IsNullOrEmpty(key) || !_managerDataDict.TryGetValue(key, out var data))
            throw new Exception($"非法的键值: {key}");

        if (!_spriteCaches.TryGetValue(key, out var spriteCache))
        {
            spriteCache = new SpriteCache();
            _spriteCaches[key] = spriteCache;
            UniTask.Void(async () =>
            {
                try
                {
                    var assetHandle = await _assetManager.LoadAssetAsync<Sprite>(data.SpriteAddress);
                    spriteCache.AssetHandle = assetHandle;
                    spriteCache.AssetCompletion.TrySetResult(assetHandle);
                }
                catch (Exception ex)
                {
                    spriteCache.AssetCompletion.TrySetException(ex);
                }
            });
        }

        if (spriteCache.AssetHandle == null)
            await spriteCache.AssetCompletion.Task;

        if (spriteCache.AssetHandle == null)
            throw new Exception("精灵句柄无效");

        var spriteHandle = new SpriteHandle(key, spriteCache.AssetHandle.Result);
        spriteCache.SpriteHandles.Add(spriteHandle);
        return spriteHandle;
    }

    public async UniTask Release(ISpriteHandle handle)
    {
        if (handle is not SpriteHandle spriteHandle) return;
        if (!_spriteCaches.TryGetValue(spriteHandle.Key, out var spriteCache)) return;
        if (!spriteCache.SpriteHandles.Remove(spriteHandle)) return;
        if (spriteCache.SpriteHandles.Count > 0) return;

        if (spriteCache.AssetHandle == null)
            await spriteCache.AssetCompletion.Task;

        if (spriteCache.AssetHandle != null)
        {
            await _assetManager.ReleaseAssetAsync(spriteCache.AssetHandle);
            _spriteCaches.Remove(spriteHandle.Key);
        }
    }

    public async UniTask UnloadAll()
    {
        foreach (var spriteCache in _spriteCaches.Values)
        {
            if (spriteCache.AssetHandle == null)
                await spriteCache.AssetCompletion.Task;

            if (spriteCache.AssetHandle != null)
                await _assetManager.ReleaseAssetAsync(spriteCache.AssetHandle);
        }

        _spriteCaches.Clear();
    }
}
