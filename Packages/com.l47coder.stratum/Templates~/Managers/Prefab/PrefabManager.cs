using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using Stratum;

public interface IPrefabHandle
{
    GameObject GameObject { get; }
}

public interface IPrefabManager
{
    UniTask<IPrefabHandle> LoadPrefabAsync(string key);
    UniTask ReleasePrefabAsync(IPrefabHandle handle);
    UniTask DestroyPoolAsync(string key);
    UniTask DestroyAllPoolAsync();
    bool TryGetHandle(GameObject gameObject, out IPrefabHandle handle);
    T AddComponent<T>(IPrefabHandle handle, string key) where T : BaseComponent;
    bool RemoveComponent<T>(IPrefabHandle handle, string key) where T : BaseComponent;
    bool TryGetComponent<T>(IPrefabHandle handle, string key, out T component) where T : BaseComponent;
    void SafeCallComponent<T>(IPrefabHandle handle, string key, Action<T> func) where T : BaseComponent;
    bool SetComponentEnabled<T>(IPrefabHandle handle, string key, bool enabled);
}

internal sealed class PrefabHandle : IPrefabHandle
{
    public string Key { get; }
    public PrefabData PrefabData { get; }
    public GameObject GameObject => PrefabData.GameObject;
    internal PrefabHandle(string key, PrefabData prefabData)
    {
        Key = key;
        PrefabData = prefabData;
    }
}

internal sealed class PrefabData
{
    public GameObject GameObject { get; }
    public Entity Entity;
    public List<string> InitialTypeKeys { get; } = new();
    public List<string> OrderedKeys { get; } = new();
    public Dictionary<string, BaseComponent> Components { get; } = new();
    public PrefabData(GameObject gameObject) => GameObject = gameObject;
}

internal sealed partial class PrefabManagerData
{
    [Field(Readonly = true)]
    public string Key;

    [Field(Readonly = true)]
    public string PrefabAddress;
    public List<ComponentRef> InitialComponent = new();
}

internal sealed partial class PrefabManager : IPrefabManager, ITickable, IAsyncInitManager
{
    private readonly Dictionary<string, PrefabManagerData> _managerDataDict = new();
    private readonly Dictionary<string, PoolCache> _poolCaches = new();
    private readonly HashSet<PrefabData> _activeInstances = new();
    private readonly Dictionary<GameObject, PrefabHandle> _goToHandle = new();

    private readonly List<PrefabData> _tickInstanceBuffer = new();
    private readonly List<string> _orderedKeyBuffer = new();
    private readonly List<BaseComponent> _dispatchBuffer = new();

    private readonly IAssetManager _assetManager;
    private readonly IComponentManager _componentManager;

    private ComponentOrderConfig _componentOrder;
    private readonly Dictionary<string, int> _typeOrderIndex = new(StringComparer.Ordinal);
    private IComparer<string> _typeKeyComparer;

    public PrefabManager(IAssetManager assetManager, IComponentManager componentManager)
    {
        _assetManager = assetManager;
        _componentManager = componentManager;
    }

    private class PoolCache
    {
        public readonly UniTaskCompletionSource<IAssetHandle<GameObject>> AssetCompletion = new();
        public readonly HashSet<IPrefabHandle> PrefabHandles = new();
        public readonly HashSet<PrefabData> AllInstances = new();
        public readonly Queue<PrefabData> InactiveQueue = new();
        public IAssetHandle<GameObject> AssetHandle;
    }

    public async UniTask InitAsync(CancellationToken token)
    {
        _componentOrder = await FrameworkLoader.LoadAsync<ComponentOrderConfig>("Frame/ComponentOrder");

        _typeOrderIndex.Clear();
        for (var i = 0; i < _componentOrder.Entries.Count; i++)
        {
            var entry = _componentOrder.Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.Name)) continue;
            _typeOrderIndex[entry.Name] = i;
        }

        _typeKeyComparer = Comparer<string>.Create(CompareTypeKey);

        WarnUnregisteredComponentTypes();
    }

    private void WarnUnregisteredComponentTypes()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = assembly.GetTypes(); }
            catch { continue; }

            foreach (var type in types)
            {
                if (type.IsAbstract) continue;
                if (!typeof(BaseComponent).IsAssignableFrom(type)) continue;
                if (!_typeOrderIndex.ContainsKey(type.Name))
                    Debug.LogWarning($"[PrefabManager] Component type is not registered in ComponentOrder and will be placed at the end: {type.Name}");
            }
        }
    }

    private int CompareTypeKey(string a, string b)
    {
        var idxA = GetTypeOrderIndex(a);
        var idxB = GetTypeOrderIndex(b);
        if (idxA != idxB) return idxA.CompareTo(idxB);
        return string.CompareOrdinal(a, b);
    }

    private int GetTypeOrderIndex(string typeKey)
    {
        var underscore = typeKey.IndexOf('_');
        var typeName = underscore < 0 ? typeKey : typeKey[..underscore];
        return _typeOrderIndex.TryGetValue(typeName, out var order) ? order : int.MaxValue;
    }

    private void InsertByOrder(PrefabData data, string typeKey)
    {
        var list = data.OrderedKeys;
        var i = 0;
        while (i < list.Count && _typeKeyComparer.Compare(list[i], typeKey) < 0) i++;
        list.Insert(i, typeKey);
    }

    public async UniTask<IPrefabHandle> LoadPrefabAsync(string key)
    {
        if (string.IsNullOrEmpty(key) || !_managerDataDict.TryGetValue(key, out var data))
            throw new Exception($"Invalid key: {key}");

        if (!_poolCaches.TryGetValue(key, out var poolCache))
        {
            poolCache = new PoolCache();
            _poolCaches[key] = poolCache;
            UniTask.Void(async () =>
            {
                try
                {
                    var assetHandle = await _assetManager.LoadAssetAsync<GameObject>(data.PrefabAddress);

                    poolCache.AssetHandle = assetHandle;
                    poolCache.AssetCompletion.TrySetResult(assetHandle);
                }
                catch (Exception ex)
                {
                    poolCache.AssetCompletion.TrySetException(ex);
                }
            });
        }

        if (poolCache.AssetHandle == null)
            await poolCache.AssetCompletion.Task;

        if (poolCache.AssetHandle == null)
            throw new Exception("Prefab handle is invalid.");

        if (poolCache.InactiveQueue.Count == 0)
            CreatePooledInstance(poolCache, data);

        var prefabData = poolCache.InactiveQueue.Dequeue();
        var prefabHandle = new PrefabHandle(key, prefabData);
        poolCache.PrefabHandles.Add(prefabHandle);
        _goToHandle[prefabData.GameObject] = prefabHandle;

        foreach (var typeKey in prefabData.InitialTypeKeys)
        {
            var comp = _componentManager.CreateComponent(typeKey);
            prefabData.Components[typeKey] = comp;
            prefabData.OrderedKeys.Add(typeKey);
        }

        foreach (var typeKey in prefabData.OrderedKeys)
            prefabData.Components[typeKey].InternalSetGameObject(prefabData.GameObject);

        _orderedKeyBuffer.Clear();
        _orderedKeyBuffer.AddRange(prefabData.OrderedKeys);
        foreach (var typeKey in _orderedKeyBuffer)
        {
            if (!prefabData.Components.TryGetValue(typeKey, out var comp)) continue;
            try { comp.InternalOnAdd(); }
            catch (Exception e) { Debug.LogException(e); }
        }
        _orderedKeyBuffer.Clear();

        _activeInstances.Add(prefabData);
        prefabData.GameObject.SetActive(true);

        _orderedKeyBuffer.Clear();
        _orderedKeyBuffer.AddRange(prefabData.OrderedKeys);
        foreach (var typeKey in _orderedKeyBuffer)
        {
            if (!prefabData.Components.TryGetValue(typeKey, out var comp)) continue;
            try { comp.InternalSetEnabled(true); }
            catch (Exception e) { Debug.LogException(e); }
        }
        _orderedKeyBuffer.Clear();

        return prefabHandle;
    }

    private void CreatePooledInstance(PoolCache poolCache, PrefabManagerData data)
    {
        var gameObject = UnityEngine.Object.Instantiate(poolCache.AssetHandle.Result);
        gameObject.SetActive(false);

        var newPrefabData = new PrefabData(gameObject);
        poolCache.AllInstances.Add(newPrefabData);

        var entity = gameObject.GetComponent<Entity>()
                  ?? gameObject.AddComponent<Entity>();
        newPrefabData.Entity = entity;

        entity.TriggerEnter     += (_, other) => DispatchTriggerEnter(newPrefabData, other);
        entity.TriggerExit      += (_, other) => DispatchTriggerExit(newPrefabData, other);
        entity.TriggerStay      += (_, other) => DispatchTriggerStay(newPrefabData, other);
        entity.CollisionEnter   += (_, c)     => DispatchCollisionEnter(newPrefabData, c);
        entity.CollisionExit    += (_, c)     => DispatchCollisionExit(newPrefabData, c);
        entity.CollisionStay    += (_, c)     => DispatchCollisionStay(newPrefabData, c);
        entity.TriggerEnter2D   += (_, other) => DispatchTriggerEnter2D(newPrefabData, other);
        entity.TriggerExit2D    += (_, other) => DispatchTriggerExit2D(newPrefabData, other);
        entity.TriggerStay2D    += (_, other) => DispatchTriggerStay2D(newPrefabData, other);
        entity.CollisionEnter2D += (_, c)     => DispatchCollisionEnter2D(newPrefabData, c);
        entity.CollisionExit2D  += (_, c)     => DispatchCollisionExit2D(newPrefabData, c);
        entity.CollisionStay2D  += (_, c)     => DispatchCollisionStay2D(newPrefabData, c);

        foreach (var compRef in data.InitialComponent)
        {
            if (!compRef.IsValid) continue;
            newPrefabData.InitialTypeKeys.Add(compRef.TypeKey);
        }
        newPrefabData.InitialTypeKeys.Sort(_typeKeyComparer);

        poolCache.InactiveQueue.Enqueue(newPrefabData);
    }

    public async UniTask ReleasePrefabAsync(IPrefabHandle handle)
    {
        if (handle is not PrefabHandle prefabHandle) return;
        if (!_poolCaches.TryGetValue(prefabHandle.Key, out var poolCache)) return;
        if (!poolCache.PrefabHandles.Contains(prefabHandle)) return;

        if (poolCache.AssetHandle == null)
            await poolCache.AssetCompletion.Task;

        if (poolCache.AssetHandle == null) return;

        var prefabData = prefabHandle.PrefabData;
        _activeInstances.Remove(prefabData);

        _orderedKeyBuffer.Clear();
        _orderedKeyBuffer.AddRange(prefabData.OrderedKeys);
        for (var i = _orderedKeyBuffer.Count - 1; i >= 0; i--)
        {
            var typeKey = _orderedKeyBuffer[i];
            if (!prefabData.Components.TryGetValue(typeKey, out var comp)) continue;
            try { comp.InternalSetEnabled(false); }
            catch (Exception e) { Debug.LogException(e); }
        }

        for (var i = _orderedKeyBuffer.Count - 1; i >= 0; i--)
        {
            var typeKey = _orderedKeyBuffer[i];
            if (!prefabData.Components.TryGetValue(typeKey, out var comp)) continue;
            try { comp.InternalOnRemove(); }
            catch (Exception e) { Debug.LogException(e); }
        }
        _orderedKeyBuffer.Clear();

        prefabData.Components.Clear();
        prefabData.OrderedKeys.Clear();

        prefabData.GameObject.SetActive(false);
        poolCache.InactiveQueue.Enqueue(prefabData);

        poolCache.PrefabHandles.Remove(prefabHandle);
        _goToHandle.Remove(prefabData.GameObject);
    }

    public async UniTask DestroyPoolAsync(string key)
    {
        if (string.IsNullOrEmpty(key) || !_managerDataDict.ContainsKey(key))
            throw new Exception($"Invalid key: {key}");

        if (!_poolCaches.TryGetValue(key, out var poolCache)) return;

        if (poolCache.AssetHandle == null)
            await poolCache.AssetCompletion.Task;

        foreach (var prefabData in poolCache.AllInstances)
        {
            var wasActive = _activeInstances.Remove(prefabData);

            _orderedKeyBuffer.Clear();
            _orderedKeyBuffer.AddRange(prefabData.OrderedKeys);

            if (wasActive)
            {
                for (var i = _orderedKeyBuffer.Count - 1; i >= 0; i--)
                {
                    var typeKey = _orderedKeyBuffer[i];
                    if (!prefabData.Components.TryGetValue(typeKey, out var comp)) continue;
                    try { comp.InternalSetEnabled(false); }
                    catch (Exception e) { Debug.LogException(e); }
                }
            }

            for (var i = _orderedKeyBuffer.Count - 1; i >= 0; i--)
            {
                var typeKey = _orderedKeyBuffer[i];
                if (!prefabData.Components.TryGetValue(typeKey, out var comp)) continue;
                try { comp.InternalOnRemove(); }
                catch (Exception e) { Debug.LogException(e); }
            }
            _orderedKeyBuffer.Clear();

            prefabData.Components.Clear();
            prefabData.OrderedKeys.Clear();
            _goToHandle.Remove(prefabData.GameObject);

            UnityEngine.Object.Destroy(prefabData.GameObject);
        }

        if (poolCache.AssetHandle != null)
            await _assetManager.ReleaseAssetAsync(poolCache.AssetHandle);

        _poolCaches.Remove(key);
    }

    public async UniTask DestroyAllPoolAsync()
    {
        foreach (var key in new List<string>(_poolCaches.Keys))
            await DestroyPoolAsync(key);
    }

    public T AddComponent<T>(IPrefabHandle handle, string key) where T : BaseComponent
    {
        var prefabData = Resolve(handle);
        var typeKey = $"{typeof(T).Name}_{key}";
        if (prefabData.Components.ContainsKey(typeKey))
            throw new Exception($"Duplicate key: {typeKey}");

        var component = _componentManager.CreateComponent<T>(key);
        prefabData.Components[typeKey] = component;
        InsertByOrder(prefabData, typeKey);
        component.InternalSetGameObject(prefabData.GameObject);
        component.InternalOnAdd();

        if (_activeInstances.Contains(prefabData))
            component.InternalSetEnabled(true);

        return component;
    }

    public bool RemoveComponent<T>(IPrefabHandle handle, string key) where T : BaseComponent
    {
        var prefabData = Resolve(handle);
        var typeKey = $"{typeof(T).Name}_{key}";
        if (!prefabData.Components.TryGetValue(typeKey, out var component)) return false;

        if (component.IsEnabled) component.InternalSetEnabled(false);
        component.InternalOnRemove();

        prefabData.Components.Remove(typeKey);
        prefabData.OrderedKeys.Remove(typeKey);
        return true;
    }

    public bool TryGetComponent<T>(IPrefabHandle handle, string key, out T component) where T : BaseComponent
    {
        var prefabData = Resolve(handle);
        var typeKey = $"{typeof(T).Name}_{key}";
        if (prefabData.Components.TryGetValue(typeKey, out var comp) && comp is T typed)
        {
            component = typed;
            return true;
        }

        component = default;
        return false;
    }

    public void SafeCallComponent<T>(IPrefabHandle handle, string key, Action<T> func) where T : BaseComponent
    {
        var prefabData = Resolve(handle);
        var typeKey = $"{typeof(T).Name}_{key}";
        if (prefabData.Components.TryGetValue(typeKey, out var comp) && comp is T typed)
            func?.Invoke(typed);
    }

    public bool SetComponentEnabled<T>(IPrefabHandle handle, string key, bool enabled)
    {
        var prefabData = Resolve(handle);
        var typeKey = $"{typeof(T).Name}_{key}";
        if (!prefabData.Components.TryGetValue(typeKey, out var component)) return false;

        component.InternalSetEnabled(enabled);
        return true;
    }

    public void Tick()
    {
        if (_activeInstances.Count == 0) return;

        _tickInstanceBuffer.Clear();
        _tickInstanceBuffer.AddRange(_activeInstances);

        foreach (var prefabData in _tickInstanceBuffer)
        {
            _orderedKeyBuffer.Clear();
            _orderedKeyBuffer.AddRange(prefabData.OrderedKeys);

            foreach (var typeKey in _orderedKeyBuffer)
            {
                if (!prefabData.Components.TryGetValue(typeKey, out var comp)) continue;
                try { comp.InternalOnUpdate(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        _orderedKeyBuffer.Clear();
        _tickInstanceBuffer.Clear();
    }

    public bool TryGetHandle(GameObject gameObject, out IPrefabHandle handle)
    {
        if (gameObject != null && _goToHandle.TryGetValue(gameObject, out var prefabHandle))
        {
            handle = prefabHandle;
            return true;
        }
        handle = null;
        return false;
    }

    private PrefabData Resolve(IPrefabHandle handle)
    {
        if (handle is not PrefabHandle prefabHandle)
            throw new InvalidOperationException("Invalid prefab handle.");

        if (!_poolCaches.TryGetValue(prefabHandle.Key, out var poolCache) ||
            !poolCache.PrefabHandles.Contains(prefabHandle))
            throw new InvalidOperationException("Prefab instance has been returned to the pool; further component operations are not allowed.");

        return prefabHandle.PrefabData;
    }

    private void FillDispatchBuffer(PrefabData data)
    {
        _dispatchBuffer.Clear();
        foreach (var typeKey in data.OrderedKeys)
        {
            if (data.Components.TryGetValue(typeKey, out var comp))
                _dispatchBuffer.Add(comp);
        }
    }

    // ── 3D 分发 ──────────────────────────────────────────────────────
    private void DispatchTriggerEnter(PrefabData data, Collider other)
    {
        if (!_activeInstances.Contains(data)) return;
        FillDispatchBuffer(data);
        foreach (var comp in _dispatchBuffer)
        {
            if (!comp.IsEnabled || comp is not IOnTriggerEnter l) continue;
            try { l.OnTriggerEntered(other); } catch (Exception e) { Debug.LogException(e); }
        }
        _dispatchBuffer.Clear();
    }

    private void DispatchTriggerExit(PrefabData data, Collider other)
    {
        if (!_activeInstances.Contains(data)) return;
        FillDispatchBuffer(data);
        foreach (var comp in _dispatchBuffer)
        {
            if (!comp.IsEnabled || comp is not IOnTriggerExit l) continue;
            try { l.OnTriggerExited(other); } catch (Exception e) { Debug.LogException(e); }
        }
        _dispatchBuffer.Clear();
    }

    private void DispatchTriggerStay(PrefabData data, Collider other)
    {
        if (!_activeInstances.Contains(data)) return;
        FillDispatchBuffer(data);
        foreach (var comp in _dispatchBuffer)
        {
            if (!comp.IsEnabled || comp is not IOnTriggerStay l) continue;
            try { l.OnTriggerStayed(other); } catch (Exception e) { Debug.LogException(e); }
        }
        _dispatchBuffer.Clear();
    }

    private void DispatchCollisionEnter(PrefabData data, Collision c)
    {
        if (!_activeInstances.Contains(data)) return;
        FillDispatchBuffer(data);
        foreach (var comp in _dispatchBuffer)
        {
            if (!comp.IsEnabled || comp is not IOnCollisionEnter l) continue;
            try { l.OnCollisionEntered(c); } catch (Exception e) { Debug.LogException(e); }
        }
        _dispatchBuffer.Clear();
    }

    private void DispatchCollisionExit(PrefabData data, Collision c)
    {
        if (!_activeInstances.Contains(data)) return;
        FillDispatchBuffer(data);
        foreach (var comp in _dispatchBuffer)
        {
            if (!comp.IsEnabled || comp is not IOnCollisionExit l) continue;
            try { l.OnCollisionExited(c); } catch (Exception e) { Debug.LogException(e); }
        }
        _dispatchBuffer.Clear();
    }

    private void DispatchCollisionStay(PrefabData data, Collision c)
    {
        if (!_activeInstances.Contains(data)) return;
        FillDispatchBuffer(data);
        foreach (var comp in _dispatchBuffer)
        {
            if (!comp.IsEnabled || comp is not IOnCollisionStay l) continue;
            try { l.OnCollisionStayed(c); } catch (Exception e) { Debug.LogException(e); }
        }
        _dispatchBuffer.Clear();
    }

    // ── 2D 分发 ──────────────────────────────────────────────────────
    private void DispatchTriggerEnter2D(PrefabData data, Collider2D other)
    {
        if (!_activeInstances.Contains(data)) return;
        FillDispatchBuffer(data);
        foreach (var comp in _dispatchBuffer)
        {
            if (!comp.IsEnabled || comp is not IOnTriggerEnter2D l) continue;
            try { l.OnTriggerEntered2D(other); } catch (Exception e) { Debug.LogException(e); }
        }
        _dispatchBuffer.Clear();
    }

    private void DispatchTriggerExit2D(PrefabData data, Collider2D other)
    {
        if (!_activeInstances.Contains(data)) return;
        FillDispatchBuffer(data);
        foreach (var comp in _dispatchBuffer)
        {
            if (!comp.IsEnabled || comp is not IOnTriggerExit2D l) continue;
            try { l.OnTriggerExited2D(other); } catch (Exception e) { Debug.LogException(e); }
        }
        _dispatchBuffer.Clear();
    }

    private void DispatchTriggerStay2D(PrefabData data, Collider2D other)
    {
        if (!_activeInstances.Contains(data)) return;
        FillDispatchBuffer(data);
        foreach (var comp in _dispatchBuffer)
        {
            if (!comp.IsEnabled || comp is not IOnTriggerStay2D l) continue;
            try { l.OnTriggerStayed2D(other); } catch (Exception e) { Debug.LogException(e); }
        }
        _dispatchBuffer.Clear();
    }

    private void DispatchCollisionEnter2D(PrefabData data, Collision2D c)
    {
        if (!_activeInstances.Contains(data)) return;
        FillDispatchBuffer(data);
        foreach (var comp in _dispatchBuffer)
        {
            if (!comp.IsEnabled || comp is not IOnCollisionEnter2D l) continue;
            try { l.OnCollisionEntered2D(c); } catch (Exception e) { Debug.LogException(e); }
        }
        _dispatchBuffer.Clear();
    }

    private void DispatchCollisionExit2D(PrefabData data, Collision2D c)
    {
        if (!_activeInstances.Contains(data)) return;
        FillDispatchBuffer(data);
        foreach (var comp in _dispatchBuffer)
        {
            if (!comp.IsEnabled || comp is not IOnCollisionExit2D l) continue;
            try { l.OnCollisionExited2D(c); } catch (Exception e) { Debug.LogException(e); }
        }
        _dispatchBuffer.Clear();
    }

    private void DispatchCollisionStay2D(PrefabData data, Collision2D c)
    {
        if (!_activeInstances.Contains(data)) return;
        FillDispatchBuffer(data);
        foreach (var comp in _dispatchBuffer)
        {
            if (!comp.IsEnabled || comp is not IOnCollisionStay2D l) continue;
            try { l.OnCollisionStayed2D(c); } catch (Exception e) { Debug.LogException(e); }
        }
        _dispatchBuffer.Clear();
    }
}
