using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Stratum;

public interface IComponentManager
{
    public T CreateComponent<T>(string key) where T : BaseComponent;
    public BaseComponent CreateComponent(string typeKey);
}

internal sealed partial class ComponentManagerData
{
    [TableColumn(Readonly = true)]
    public string Key;

    [TableColumn(Readonly = true)]
    public string ComponentConfigAddress;
}

internal sealed partial class ComponentManager : IComponentManager, IAsyncInitManager
{
    private readonly Dictionary<string, ComponentManagerData> _managerDataDict = new();
    private readonly Dictionary<string, BaseComponentData> _componentDataDict = new();

    public async UniTask InitAsync(CancellationToken token)
    {
        _componentDataDict.Clear();
        foreach (var data in _managerDataDict.Values)
        {
            var componentConfig = await FrameworkLoader.LoadAsync<BaseComponentConfig>(data.ComponentConfigAddress);
            foreach (var kv in componentConfig.ExportComponentDataDict())
                _componentDataDict[kv.Key] = kv.Value;
        }
    }

    public T CreateComponent<T>(string key) where T : BaseComponent
    {
        var typeKey = $"{typeof(T).Name}_{key}";
        var component = CreateComponent(typeKey);
        if (component is not T result)
            throw new Exception($"Component type mismatch: {typeKey}");

        return result;
    }

    public BaseComponent CreateComponent(string typeKey)
    {
        if (string.IsNullOrEmpty(typeKey) || !_componentDataDict.TryGetValue(typeKey, out var componentData))
            throw new Exception($"Invalid key: {typeKey}");

        return componentData.InternalCreateComponent();
    }
}
