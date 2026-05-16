using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public interface IAssetManager
{
    public UniTask<T> LoadAsync<T>(string address) where T : class;
    public UniTask ReleaseAllAsync();
}

internal sealed partial class AssetManager : IAssetManager
{
    private readonly Dictionary<string, AssetCache> _assetCaches = new(StringComparer.Ordinal);

    private sealed class AssetCache
    {
        public readonly UniTaskCompletionSource<AsyncOperationHandle> OperationCompletion = new();
        public AsyncOperationHandle OperationHandle;
    }

    public async UniTask<T> LoadAsync<T>(string address) where T : class
    {
        if (string.IsNullOrEmpty(address))
            throw new Exception($"Invalid address: {address}");

        var assetCache = GetOrCreateCache<T>(address);
        if (!assetCache.OperationHandle.IsValid())
            await assetCache.OperationCompletion.Task;

        if (!assetCache.OperationHandle.IsValid())
            throw new Exception("Asset handle is invalid.");

        if (assetCache.OperationHandle.Status != AsyncOperationStatus.Succeeded)
            throw new Exception("Asset load failed.");

        if (assetCache.OperationHandle.Result is not T result)
            throw new Exception("Asset type mismatch.");

        return result;
    }

    public async UniTask ReleaseAllAsync()
    {
        var assetCaches = new List<AssetCache>(_assetCaches.Values);
        _assetCaches.Clear();

        foreach (var assetCache in assetCaches)
        {
            try
            {
                if (!assetCache.OperationHandle.IsValid())
                    await assetCache.OperationCompletion.Task;
            }
            catch
            {
                continue;
            }

            if (assetCache.OperationHandle.IsValid())
                Addressables.Release(assetCache.OperationHandle);
        }
    }

    private AssetCache GetOrCreateCache<T>(string address) where T : class
    {
        var cacheKey = GetCacheKey<T>(address);
        if (_assetCaches.TryGetValue(cacheKey, out var assetCache))
            return assetCache;

        assetCache = new AssetCache();
        _assetCaches[cacheKey] = assetCache;

        UniTask.Void(async () =>
        {
            try
            {
                var operationHandle = Addressables.LoadAssetAsync<T>(address);
                await operationHandle.ToUniTask();

                assetCache.OperationHandle = operationHandle;
                assetCache.OperationCompletion.TrySetResult(operationHandle);
            }
            catch (Exception ex)
            {
                _assetCaches.Remove(cacheKey);
                assetCache.OperationCompletion.TrySetException(ex);
            }
        });

        return assetCache;
    }

    private static string GetCacheKey<T>(string address) where T : class => $"{typeof(T).AssemblyQualifiedName}:{address}";
}
