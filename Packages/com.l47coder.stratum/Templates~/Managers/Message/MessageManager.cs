using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Stratum;
using UnityEngine;

public interface IMessageManager
{
    public void Send(string id);
    public void Send<TParam>(string id, TParam parameter);
    public TResult Send<TResult>(string id);
    public TResult Send<TParam, TResult>(string id, TParam parameter);
    public UniTask SendAsync(string id);
    public UniTask SendAsync<TParam>(string id, TParam parameter);
    public UniTask<TResult> SendAsync<TResult>(string id);
    public UniTask<TResult> SendAsync<TParam, TResult>(string id, TParam parameter);
    public void Receive(string id, Action callback);
    public void Receive<TParam>(string id, Action<TParam> callback);
    public void Receive<TResult>(string id, Func<TResult> callback);
    public void Receive<TParam, TResult>(string id, Func<TParam, TResult> callback);
    public void ReceiveAsync(string id, Func<CancellationToken, UniTask> callback);
    public void ReceiveAsync<TParam>(string id, Func<TParam, CancellationToken, UniTask> callback);
    public void ReceiveAsync<TResult>(string id, Func<CancellationToken, UniTask<TResult>> callback);
    public void ReceiveAsync<TParam, TResult>(string id, Func<TParam, CancellationToken, UniTask<TResult>> callback);
    public bool Unreceive(string id);
    public void UnreceiveAll();
}

internal sealed partial class MessageManager : IMessageManager
{
    private readonly object _lock = new();
    private readonly Dictionary<string, Delegate> _callbacks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CancellationTokenSource> _groupSources = new(StringComparer.Ordinal);

    public void Send(string id)
    {
        if (!TryGetCallback(id, out Action callback)) return;
        Invoke(id, callback);
    }

    public void Send<TParam>(string id, TParam parameter)
    {
        if (!TryGetCallback(id, out Action<TParam> callback)) return;
        Invoke(id, () => callback(parameter));
    }

    public TResult Send<TResult>(string id)
    {
        if (!TryGetCallback(id, out Func<TResult> callback)) return default;
        return Invoke(id, callback);
    }

    public TResult Send<TParam, TResult>(string id, TParam parameter)
    {
        if (!TryGetCallback(id, out Func<TParam, TResult> callback)) return default;
        return Invoke(id, () => callback(parameter));
    }

    public async UniTask SendAsync(string id)
    {
        if (!TryGetCallback(id, out Func<CancellationToken, UniTask> callback)) return;
        await InvokeAsync(id, callback);
    }

    public async UniTask SendAsync<TParam>(string id, TParam parameter)
    {
        if (!TryGetCallback(id, out Func<TParam, CancellationToken, UniTask> callback)) return;
        await InvokeAsync(id, token => callback(parameter, token));
    }

    public async UniTask<TResult> SendAsync<TResult>(string id)
    {
        if (!TryGetCallback(id, out Func<CancellationToken, UniTask<TResult>> callback)) return default;
        return await InvokeAsync(id, callback);
    }

    public async UniTask<TResult> SendAsync<TParam, TResult>(string id, TParam parameter)
    {
        if (!TryGetCallback(id, out Func<TParam, CancellationToken, UniTask<TResult>> callback)) return default;
        return await InvokeAsync(id, token => callback(parameter, token));
    }

    public void Receive(string id, Action callback) => AddCallback(id, callback);

    public void Receive<TParam>(string id, Action<TParam> callback) => AddCallback(id, callback);

    public void Receive<TResult>(string id, Func<TResult> callback) => AddCallback(id, callback);

    public void Receive<TParam, TResult>(string id, Func<TParam, TResult> callback) => AddCallback(id, callback);

    public void ReceiveAsync(string id, Func<CancellationToken, UniTask> callback) => AddCallback(id, callback);

    public void ReceiveAsync<TParam>(string id, Func<TParam, CancellationToken, UniTask> callback) => AddCallback(id, callback);

    public void ReceiveAsync<TResult>(string id, Func<CancellationToken, UniTask<TResult>> callback) => AddCallback(id, callback);

    public void ReceiveAsync<TParam, TResult>(string id, Func<TParam, CancellationToken, UniTask<TResult>> callback) => AddCallback(id, callback);

    public bool Unreceive(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        lock (_lock)
            return _callbacks.Remove(id);
    }

    public void UnreceiveAll()
    {
        CancellationTokenSource[] groupSources;
        lock (_lock)
        {
            groupSources = new CancellationTokenSource[_groupSources.Count];
            _groupSources.Values.CopyTo(groupSources, 0);
            _callbacks.Clear();
            _groupSources.Clear();
        }

        foreach (var groupSource in groupSources)
            groupSource.Cancel();
    }

    private void AddCallback(string id, Delegate callback)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("消息 ID 不能为空", nameof(id));

        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        lock (_lock)
            _callbacks[id] = callback;
    }

    private bool TryGetCallback<TCallback>(string id, out TCallback callback) where TCallback : Delegate
    {
        callback = null;
        if (string.IsNullOrEmpty(id)) return false;

        Delegate value;
        lock (_lock)
        {
            if (!_callbacks.TryGetValue(id, out value))
                value = null;
        }

        if (value is TCallback typedCallback)
        {
            callback = typedCallback;
            return true;
        }

        Debug.LogWarning($"消息回调未找到: {id}");
        return false;
    }

    private static void Invoke(string id, Action callback)
    {
        try
        {
            callback();
        }
        catch (Exception ex)
        {
            Debug.LogError($"消息处理器执行失败 [{id}]: {ex.Message}");
        }
    }

    private static TResult Invoke<TResult>(string id, Func<TResult> callback)
    {
        try
        {
            return callback();
        }
        catch (Exception ex)
        {
            Debug.LogError($"消息处理器执行失败 [{id}]: {ex.Message}");
            return default;
        }
    }

    private async UniTask InvokeAsync(string id, Func<CancellationToken, UniTask> callback)
    {
        var groupId = Guid.NewGuid().ToString("N");
        using var groupSource = new CancellationTokenSource();
        AddGroupSource(groupId, groupSource);

        try
        {
            await callback(groupSource.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.LogError($"消息处理器执行失败 [{id}]: {ex.Message}");
        }
        finally
        {
            RemoveGroupSource(groupId);
        }
    }

    private async UniTask<TResult> InvokeAsync<TResult>(string id, Func<CancellationToken, UniTask<TResult>> callback)
    {
        var groupId = Guid.NewGuid().ToString("N");
        using var groupSource = new CancellationTokenSource();
        AddGroupSource(groupId, groupSource);

        try
        {
            return await callback(groupSource.Token);
        }
        catch (OperationCanceledException)
        {
            return default;
        }
        catch (Exception ex)
        {
            Debug.LogError($"消息处理器执行失败 [{id}]: {ex.Message}");
            return default;
        }
        finally
        {
            RemoveGroupSource(groupId);
        }
    }

    private void AddGroupSource(string groupId, CancellationTokenSource groupSource)
    {
        lock (_lock)
            _groupSources[groupId] = groupSource;
    }

    private void RemoveGroupSource(string groupId)
    {
        lock (_lock)
            _groupSources.Remove(groupId);
    }
}
