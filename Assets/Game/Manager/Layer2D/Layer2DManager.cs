using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Stratum;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public enum Layer2DRootType
{
    Object,
    Canvas,
}

public interface ILayer2DManager
{
    public void SetLayer(Transform tr, Layer2DRootType root, int layer);
}

internal sealed partial class Layer2DManagerData
{
    [Field(Readonly = true, Width = 300)]
    public string Key;
}

internal sealed partial class Layer2DManager : ILayer2DManager, IAsyncInitManager
{
    private const string ManagerRootName = "Layer2DRoot";
    private const string ObjectRootName = "ObjectRoot";
    private const string CanvasRootName = "CanvasRoot";

    private readonly Dictionary<string, Layer2DManagerData> _managerDataDict = new(StringComparer.Ordinal);
    private readonly Dictionary<int, Transform> _objectNodes = new();
    private readonly Dictionary<int, Transform> _canvasNodes = new();
    private Transform _managerRoot;
    private Transform _objectRoot;
    private Transform _canvasRoot;

    public async UniTask InitAsync(CancellationToken token)
    {
        _managerRoot = CreateRoot(ManagerRootName);
        _objectRoot = CreateRoot(ObjectRootName, _managerRoot);
        _canvasRoot = CreateRoot(CanvasRootName, _managerRoot);
        await UniTask.CompletedTask;
    }

    public void SetLayer(Transform tr, Layer2DRootType root, int layer)
    {
        if (tr == null) return;
        var parent = root switch
        {
            Layer2DRootType.Object => GetOrCreateObjectLayerNode(layer),
            Layer2DRootType.Canvas => GetOrCreateCanvasLayerNode(layer),
            _ => throw new ArgumentOutOfRangeException(nameof(root), root, null),
        };
        tr.SetParent(parent, false);
    }

    private Transform GetOrCreateObjectLayerNode(int layer) =>
        _objectNodes.TryGetValue(layer, out var node) ? node : CreateObjectLayerNode(layer);

    private Transform CreateObjectLayerNode(int layer)
    {
        var go = new GameObject(layer.ToString());
        var sortingGroup = go.AddComponent<SortingGroup>();
        sortingGroup.sortingOrder = layer;
        go.transform.SetParent(_objectRoot, false);
        SortRootChildren(_objectRoot);
        var tr = go.transform;
        _objectNodes[layer] = tr;
        return tr;
    }

    private Transform GetOrCreateCanvasLayerNode(int layer) =>
        _canvasNodes.TryGetValue(layer, out var node) ? node : CreateCanvasLayerNode(layer);

    private Transform CreateCanvasLayerNode(int layer)
    {
        var go = new GameObject(layer.ToString());
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = layer;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560, 1600);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        go.transform.SetParent(_canvasRoot, false);
        SortRootChildren(_canvasRoot);
        var tr = go.transform;
        _canvasNodes[layer] = tr;
        return tr;
    }

    private static Transform CreateRoot(string name, Transform parent = null)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        return root;
    }

    private static void SortRootChildren(Transform root)
    {
        if (root == null || root.childCount == 0) return;

        var sortedChildren = root
            .Cast<Transform>()
            .OrderBy(static child => int.TryParse(child.name, out var layer) ? layer : int.MaxValue)
            .ToList();

        for (var i = 0; i < sortedChildren.Count; i++)
            sortedChildren[i].SetSiblingIndex(i);
    }
}
