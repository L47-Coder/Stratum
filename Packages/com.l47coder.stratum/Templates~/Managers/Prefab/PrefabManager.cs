using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Stratum;
using UnityEngine;
using VContainer;
using VContainer.Unity;

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
    public Dictionary<string, EntityComponentEntry> EntityEntryByKey { get; } = new(StringComparer.Ordinal);
    public List<string> InitialTypeKeys { get; } = new();

    public List<string> OrderedKeys { get; } = new();
    public Dictionary<string, BaseComponent> Components { get; } = new(StringComparer.Ordinal);
    public PrefabData(GameObject gameObject) => GameObject = gameObject;
}

internal sealed partial class PrefabManagerData
{
    [Field(Readonly = true, Width = 300)]
    public string Key;

    [Field(Readonly = true)]
    public string PrefabAddress;
}

internal sealed partial class PrefabManager : IPrefabManager, ITickable, IAsyncInitManager
{
    private readonly Dictionary<string, PrefabManagerData> _managerDataDict = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PoolCache> _poolCaches = new();
    private readonly HashSet<PrefabData> _activeInstances = new();
    private readonly Dictionary<GameObject, PrefabHandle> _goToHandle = new();

    private readonly List<PrefabData> _tickInstanceBuffer = new();
    private readonly List<string> _orderedKeyBuffer = new();
    private readonly List<string> _loadPrefabEnableKeyBuffer = new();
    private readonly List<string> _clearComponentsKeyBuffer = new();
    private readonly List<string> _dispatchKeyBuffer = new();
    private readonly List<BaseComponent> _dispatchBuffer = new();

    private readonly IAssetManager _assetManager;
    private readonly IComponentManager _componentManager;
    private readonly IObjectResolver _container;

    private ComponentOrderConfig _componentOrder;
    private readonly Dictionary<string, int> _typeOrderIndex = new(StringComparer.Ordinal);
    private IComparer<string> _typeKeyComparer;

    public PrefabManager(IAssetManager assetManager, IComponentManager componentManager, IObjectResolver container)
    {
        _assetManager = assetManager;
        _componentManager = componentManager;
        _container = container;
    }

    private sealed class PoolCache
    {
        public readonly UniTaskCompletionSource<IAssetHandle<GameObject>> AssetCompletion = new();
        public readonly HashSet<IPrefabHandle> PrefabHandles = new();
        public readonly HashSet<PrefabData> AllInstances = new();
        public readonly Queue<PrefabData> InactiveQueue = new();
        public IAssetHandle<GameObject> AssetHandle;
        public Vector3 InitialLocalPosition;
        public Quaternion InitialLocalRotation;
        public Vector3 InitialLocalScale = Vector3.one;
        public bool HasInitialTransform;
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
                    CaptureInitialTransform(poolCache, assetHandle.Result);

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
            CreatePooledInstance(poolCache);

        var prefabData = poolCache.InactiveQueue.Dequeue();
        var prefabHandle = new PrefabHandle(key, prefabData);
        poolCache.PrefabHandles.Add(prefabHandle);
        _goToHandle[prefabData.GameObject] = prefabHandle;

        ApplyInitialTransform(poolCache, prefabData);
        BuildInitialComponents(prefabData);

        _activeInstances.Add(prefabData);
        prefabData.GameObject.SetActive(true);

        _loadPrefabEnableKeyBuffer.Clear();
        _loadPrefabEnableKeyBuffer.AddRange(prefabData.OrderedKeys);
        foreach (var typeKey in _loadPrefabEnableKeyBuffer)
        {
            if (!prefabData.Components.TryGetValue(typeKey, out var comp)) continue;
            try { comp.InternalSetEnabled(true); }
            catch (Exception e) { Debug.LogException(e); }
        }
        _loadPrefabEnableKeyBuffer.Clear();

        return prefabHandle;
    }

    private void CreatePooledInstance(PoolCache poolCache)
    {
        var gameObject = UnityEngine.Object.Instantiate(poolCache.AssetHandle.Result);
        gameObject.SetActive(false);

        var newPrefabData = new PrefabData(gameObject);
        poolCache.AllInstances.Add(newPrefabData);

        if (!gameObject.TryGetComponent<Entity>(out var entity))
            entity = gameObject.AddComponent<Entity>();
        newPrefabData.Entity = entity;

        entity.TriggerEnter += (_, other) => DispatchTriggerEnter(newPrefabData, other);
        entity.TriggerExit += (_, other) => DispatchTriggerExit(newPrefabData, other);
        entity.TriggerStay += (_, other) => DispatchTriggerStay(newPrefabData, other);
        entity.CollisionEnter += (_, c) => DispatchCollisionEnter(newPrefabData, c);
        entity.CollisionExit += (_, c) => DispatchCollisionExit(newPrefabData, c);
        entity.CollisionStay += (_, c) => DispatchCollisionStay(newPrefabData, c);
        entity.TriggerEnter2D += (_, other) => DispatchTriggerEnter2D(newPrefabData, other);
        entity.TriggerExit2D += (_, other) => DispatchTriggerExit2D(newPrefabData, other);
        entity.TriggerStay2D += (_, other) => DispatchTriggerStay2D(newPrefabData, other);
        entity.CollisionEnter2D += (_, c) => DispatchCollisionEnter2D(newPrefabData, c);
        entity.CollisionExit2D += (_, c) => DispatchCollisionExit2D(newPrefabData, c);
        entity.CollisionStay2D += (_, c) => DispatchCollisionStay2D(newPrefabData, c);

        foreach (var componentEntry in entity.Components)
        {
            if (componentEntry == null) continue;
            if (string.IsNullOrEmpty(componentEntry.EntryKey)) continue;
            if (newPrefabData.EntityEntryByKey.ContainsKey(componentEntry.EntryKey))
            {
                Debug.LogWarning($"[PrefabManager] Duplicate Entity entry key '{componentEntry.EntryKey}' ignored on prefab '{gameObject.name}'.");
                continue;
            }
            newPrefabData.EntityEntryByKey[componentEntry.EntryKey] = componentEntry;

            if (!componentEntry.InitOnStart) continue;
            if (componentEntry.Data == null) continue;
            newPrefabData.InitialTypeKeys.Add(componentEntry.EntryKey);
        }
        newPrefabData.InitialTypeKeys.Sort(_typeKeyComparer);

        ApplyInitialTransform(poolCache, newPrefabData);
        poolCache.InactiveQueue.Enqueue(newPrefabData);
    }

    private static void CaptureInitialTransform(PoolCache poolCache, GameObject prefab)
    {
        if (poolCache == null || prefab == null) return;
        var t = prefab.transform;
        poolCache.InitialLocalPosition = t.localPosition;
        poolCache.InitialLocalRotation = t.localRotation;
        poolCache.InitialLocalScale = t.localScale;
        poolCache.HasInitialTransform = true;
    }

    private static void ApplyInitialTransform(PoolCache poolCache, PrefabData prefabData)
    {
        if (poolCache == null || prefabData?.GameObject == null || !poolCache.HasInitialTransform) return;
        var t = prefabData.GameObject.transform;
        t.SetParent(null, false);
        t.SetLocalPositionAndRotation(poolCache.InitialLocalPosition, poolCache.InitialLocalRotation);
        t.localScale = poolCache.InitialLocalScale;
    }

    private void ClearAllComponents(PrefabData prefabData)
    {
        _clearComponentsKeyBuffer.Clear();
        _clearComponentsKeyBuffer.AddRange(prefabData.OrderedKeys);
        for (var i = _clearComponentsKeyBuffer.Count - 1; i >= 0; i--)
        {
            var typeKey = _clearComponentsKeyBuffer[i];
            if (!prefabData.Components.TryGetValue(typeKey, out var comp)) continue;
            if (comp.IsEnabled)
            {
                try { comp.InternalSetEnabled(false); }
                catch (Exception e) { Debug.LogException(e); }
            }
            try { comp.InternalOnRemove(); }
            catch (Exception e) { Debug.LogException(e); }
        }
        _clearComponentsKeyBuffer.Clear();
        prefabData.Components.Clear();
        prefabData.OrderedKeys.Clear();
    }

    private void BuildInitialComponents(PrefabData prefabData)
    {
        if (prefabData.Components.Count > 0 || prefabData.OrderedKeys.Count > 0)
            ClearAllComponents(prefabData);

        foreach (var typeKey in prefabData.InitialTypeKeys)
        {
            if (!prefabData.EntityEntryByKey.TryGetValue(typeKey, out var componentEntry)) continue;
            if (componentEntry?.Data == null) continue;

            BaseComponent component;
            try { component = componentEntry.Data.InternalCreateComponent(); }
            catch (Exception e) { Debug.LogException(e); continue; }
            if (component == null) continue;

            prefabData.Components[typeKey] = component;
            InsertByOrder(prefabData, typeKey);
            component.InternalSetGameObject(prefabData.GameObject);
            try { _container.Inject(component); }
            catch (Exception e) { Debug.LogException(e); }
            try { component.InternalOnAdd(); }
            catch (Exception e) { Debug.LogException(e); }
        }
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

        ClearAllComponents(prefabData);
        ApplyInitialTransform(poolCache, prefabData);

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
            _activeInstances.Remove(prefabData);

            ClearAllComponents(prefabData);
            prefabData.EntityEntryByKey.Clear();
            prefabData.InitialTypeKeys.Clear();
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

        T component;
        if (prefabData.EntityEntryByKey.TryGetValue(typeKey, out var componentEntry) && componentEntry?.Data != null)
        {
            var created = componentEntry.Data.InternalCreateComponent();
            if (created is not T typed)
                throw new Exception($"Component type mismatch on Entity entry: {typeKey}");
            component = typed;
        }
        else
        {
            component = _componentManager.CreateComponent<T>(key);
        }

        prefabData.Components[typeKey] = component;
        InsertByOrder(prefabData, typeKey);
        component.InternalSetGameObject(prefabData.GameObject);
        _container.Inject(component);
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
        _dispatchKeyBuffer.Clear();
        _dispatchKeyBuffer.AddRange(data.OrderedKeys);
        foreach (var typeKey in _dispatchKeyBuffer)
        {
            if (data.Components.TryGetValue(typeKey, out var comp))
                _dispatchBuffer.Add(comp);
        }
        _dispatchKeyBuffer.Clear();
    }

    private void DispatchTriggerEnter(PrefabData data, Collider other) =>
        Dispatch<IOnTriggerEnter>(data, listener => listener.OnTriggerEntered(other));

    private void DispatchTriggerExit(PrefabData data, Collider other) =>
        Dispatch<IOnTriggerExit>(data, listener => listener.OnTriggerExited(other));

    private void DispatchTriggerStay(PrefabData data, Collider other) =>
        Dispatch<IOnTriggerStay>(data, listener => listener.OnTriggerStayed(other));

    private void DispatchCollisionEnter(PrefabData data, Collision c) =>
        Dispatch<IOnCollisionEnter>(data, listener => listener.OnCollisionEntered(c));

    private void DispatchCollisionExit(PrefabData data, Collision c) =>
        Dispatch<IOnCollisionExit>(data, listener => listener.OnCollisionExited(c));

    private void DispatchCollisionStay(PrefabData data, Collision c) =>
        Dispatch<IOnCollisionStay>(data, listener => listener.OnCollisionStayed(c));

    private void DispatchTriggerEnter2D(PrefabData data, Collider2D other) =>
        Dispatch<IOnTriggerEnter2D>(data, listener => listener.OnTriggerEntered2D(other));

    private void DispatchTriggerExit2D(PrefabData data, Collider2D other) =>
        Dispatch<IOnTriggerExit2D>(data, listener => listener.OnTriggerExited2D(other));

    private void DispatchTriggerStay2D(PrefabData data, Collider2D other) =>
        Dispatch<IOnTriggerStay2D>(data, listener => listener.OnTriggerStayed2D(other));

    private void DispatchCollisionEnter2D(PrefabData data, Collision2D c) =>
        Dispatch<IOnCollisionEnter2D>(data, listener => listener.OnCollisionEntered2D(c));

    private void DispatchCollisionExit2D(PrefabData data, Collision2D c) =>
        Dispatch<IOnCollisionExit2D>(data, listener => listener.OnCollisionExited2D(c));

    private void DispatchCollisionStay2D(PrefabData data, Collision2D c) =>
        Dispatch<IOnCollisionStay2D>(data, listener => listener.OnCollisionStayed2D(c));

    private void Dispatch<TListener>(PrefabData data, Action<TListener> invoke)
    {
        if (!_activeInstances.Contains(data)) return;

        FillDispatchBuffer(data);
        foreach (var comp in _dispatchBuffer)
        {
            if (!comp.IsEnabled || comp is not TListener listener) continue;

            try
            {
                invoke(listener);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        _dispatchBuffer.Clear();
    }
}
