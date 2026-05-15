using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Stratum;
using UnityEngine;

public interface IEventHandle { }

public interface IEventManager
{
    public void Publish<T>(T message);
    public UniTask PublishAsync<T>(T message, int timeoutMs = 10000);
    public IEventHandle Subscribe<T>(Action<T> callback);
    public IEventHandle SubscribeAsync<T>(Func<T, CancellationToken, UniTask> callback);
    public void Unsubscribe(IEventHandle handle);
    public void UnsubscribeAll();
}

internal sealed class EventHandle : IEventHandle
{
    public string Key { get; }

    public EventHandle(string key) => Key = key;
}

internal sealed partial class EventManager : IEventManager
{
    private readonly object _lock = new();
    private readonly Dictionary<string, EventManagerData> _managerDataDict = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, Dictionary<string, Delegate>> _callbacks = new();
    private readonly Dictionary<string, Type> _keyToType = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CancellationTokenSource> _groupSources = new(StringComparer.Ordinal);

    public void Publish<T>(T message)
    {
        foreach (var callback in GetCallbacks<Action<T>>(typeof(T)))
        {
            try
            {
                callback(message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"同步事件处理失败: {ex.Message}");
            }
        }
    }

    public async UniTask PublishAsync<T>(T message, int timeoutMs = 10000)
    {
        var callbacks = GetCallbacks<Func<T, CancellationToken, UniTask>>(typeof(T));
        if (callbacks.Length == 0) return;

        var groupId = Guid.NewGuid().ToString("N");
        using var groupSource = new CancellationTokenSource();
        AddGroupSource(groupId, groupSource);

        try
        {
            var publishTask = UniTask.WhenAll(callbacks
                .Select(callback => InvokeAsync(callback, message, groupSource.Token))
                .ToArray());

            if (await UniTask.WhenAny(publishTask, UniTask.Delay(timeoutMs)) == 1)
            {
                Debug.LogWarning($"异步事件处理超时 ({timeoutMs}ms)");
                groupSource.Cancel();
                return;
            }

            await publishTask;
        }
        finally
        {
            RemoveGroupSource(groupId);
        }
    }

    public IEventHandle Subscribe<T>(Action<T> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        return AddCallback(typeof(T), callback);
    }

    public IEventHandle SubscribeAsync<T>(Func<T, CancellationToken, UniTask> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        return AddCallback(typeof(T), callback);
    }

    public void Unsubscribe(IEventHandle handle)
    {
        if (handle is not EventHandle eventHandle) return;

        lock (_lock)
        {
            if (!_keyToType.TryGetValue(eventHandle.Key, out var type)) return;
            _keyToType.Remove(eventHandle.Key);

            if (!_callbacks.TryGetValue(type, out var callbacks)) return;
            callbacks.Remove(eventHandle.Key);

            if (callbacks.Count == 0)
                _callbacks.Remove(type);
        }
    }

    public void UnsubscribeAll()
    {
        CancellationTokenSource[] groupSources;
        lock (_lock)
        {
            groupSources = _groupSources.Values.ToArray();
            _callbacks.Clear();
            _keyToType.Clear();
            _groupSources.Clear();
        }

        foreach (var groupSource in groupSources)
            groupSource.Cancel();
    }

    private EventHandle AddCallback(Type type, Delegate callback)
    {
        var key = Guid.NewGuid().ToString("N");
        var eventHandle = new EventHandle(key);

        lock (_lock)
        {
            _keyToType[key] = type;

            if (!_callbacks.TryGetValue(type, out var callbackDict))
            {
                callbackDict = new Dictionary<string, Delegate>(StringComparer.Ordinal);
                _callbacks[type] = callbackDict;
            }

            callbackDict[key] = callback;
        }

        return eventHandle;
    }

    private TCallback[] GetCallbacks<TCallback>(Type type)
    {
        lock (_lock)
        {
            return _callbacks.TryGetValue(type, out var callbackDict)
                ? callbackDict.Values.OfType<TCallback>().ToArray()
                : Array.Empty<TCallback>();
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

    private static async UniTask InvokeAsync<T>(Func<T, CancellationToken, UniTask> callback, T message, CancellationToken token)
    {
        try
        {
            await callback(message, token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.LogWarning($"异步事件处理失败: {ex.Message}");
        }
    }
}
