using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Stratum;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public interface IAssetHandle<T> where T : class
{
    public T Result { get; }
}

public interface IAssetManager
{
    public UniTask<IAssetHandle<T>> LoadAssetAsync<T>(string key) where T : class;
    public UniTask ReleaseAssetAsync<T>(IAssetHandle<T> handle) where T : class;
    public UniTask ReleaseAllAssetAsync();
}

internal sealed class AssetHandle<T> : IAssetHandle<T> where T : class
{
    public string Key { get; }
    public T Result { get; }

    public AssetHandle(string key, T result)
    {
        Key = key;
        Result = result;
    }
}

internal sealed partial class AssetManager : IAssetManager
{
    private readonly Dictionary<string, AssetCache> _assetCaches = new(StringComparer.Ordinal);

    private sealed class AssetCache
    {
        public readonly UniTaskCompletionSource<AsyncOperationHandle> OperationCompletion = new();
        public readonly HashSet<object> AssetHandles = new();
        public AsyncOperationHandle OperationHandle;
    }

    public async UniTask<IAssetHandle<T>> LoadAssetAsync<T>(string key) where T : class
    {
        if (string.IsNullOrEmpty(key) || !_managerDataDict.TryGetValue(key, out var data))
            throw new Exception($"Invalid key: {key}");

        var assetCache = GetOrCreateCache<T>(key, data.AssetAddress);
        if (!assetCache.OperationHandle.IsValid())
            await assetCache.OperationCompletion.Task;

        if (!assetCache.OperationHandle.IsValid())
            throw new Exception("Asset handle is invalid.");

        if (assetCache.OperationHandle.Status != AsyncOperationStatus.Succeeded)
            throw new Exception("Asset load failed.");

        if (assetCache.OperationHandle.Result is not T result)
            throw new Exception("Asset type mismatch.");

        var assetHandle = new AssetHandle<T>(key, result);
        assetCache.AssetHandles.Add(assetHandle);
        return assetHandle;
    }

    public async UniTask ReleaseAssetAsync<T>(IAssetHandle<T> handle) where T : class
    {
        if (handle is not AssetHandle<T> assetHandle) return;
        if (!_assetCaches.TryGetValue(assetHandle.Key, out var assetCache)) return;
        if (!assetCache.AssetHandles.Remove(assetHandle)) return;
        if (assetCache.AssetHandles.Count > 0) return;

        if (!assetCache.OperationHandle.IsValid())
            await assetCache.OperationCompletion.Task;

        if (!assetCache.OperationHandle.IsValid()) return;

        Addressables.Release(assetCache.OperationHandle);
        _assetCaches.Remove(assetHandle.Key);
    }

    public async UniTask ReleaseAllAssetAsync()
    {
        foreach (var assetCache in _assetCaches.Values)
        {
            if (!assetCache.OperationHandle.IsValid())
                await assetCache.OperationCompletion.Task;

            if (assetCache.OperationHandle.IsValid())
                Addressables.Release(assetCache.OperationHandle);
        }

        _assetCaches.Clear();
    }

    private AssetCache GetOrCreateCache<T>(string key, string assetAddress) where T : class
    {
        if (_assetCaches.TryGetValue(key, out var assetCache))
            return assetCache;

        assetCache = new AssetCache();
        _assetCaches[key] = assetCache;

        UniTask.Void(async () =>
        {
            try
            {
                var operationHandle = Addressables.LoadAssetAsync<T>(assetAddress);
                await operationHandle.ToUniTask();

                assetCache.OperationHandle = operationHandle;
                assetCache.OperationCompletion.TrySetResult(operationHandle);
            }
            catch (Exception ex)
            {
                assetCache.OperationCompletion.TrySetException(ex);
            }
        });

        return assetCache;
    }
}
