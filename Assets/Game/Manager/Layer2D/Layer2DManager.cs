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
    private readonly Dictionary<string, Layer2DManagerData> _managerDataDict = new(StringComparer.Ordinal);
    private readonly Dictionary<int, Transform> _objectNodes = new();
    private readonly Dictionary<int, Transform> _canvasNodes = new();
    private Transform _objectRoot;
    private Transform _canvasRoot;

    public async UniTask InitAsync(CancellationToken token)
    {
        _objectRoot = GameObject.Find("ObjectRoot")?.transform;
        _canvasRoot = GameObject.Find("CanvasRoot")?.transform;
        await UniTask.CompletedTask;
    }

    public void SetLayer(Transform tr, Layer2DRootType root, int layer)
    {
        if (tr == null) return;

        switch (root)
        {
            case Layer2DRootType.Object:
                SetObjectLayer(tr, layer);
                break;
            case Layer2DRootType.Canvas:
                SetCanvasLayer(tr, layer);
                break;
        }
    }

    private void SetObjectLayer(Transform tr, int layer)
    {
        if (_objectRoot == null)
        {
            Debug.LogWarning("[Layer2DManager] ObjectRoot not found.");
            return;
        }

        if (!_objectNodes.TryGetValue(layer, out var node))
        {
            var nodeObject = new GameObject(layer.ToString());
            var sortingGroup = nodeObject.AddComponent<SortingGroup>();
            sortingGroup.sortingOrder = layer;

            nodeObject.transform.SetParent(_objectRoot, false);
            SortRootChildren(_objectRoot);
            _objectNodes[layer] = node = nodeObject.transform;
        }

        tr.SetParent(node, false);
    }

    private void SetCanvasLayer(Transform tr, int layer)
    {
        if (_canvasRoot == null)
        {
            Debug.LogWarning("[Layer2DManager] CanvasRoot not found.");
            return;
        }

        if (!_canvasNodes.TryGetValue(layer, out var node))
        {
            var nodeObject = new GameObject(layer.ToString());
            var canvas = nodeObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = layer;

            var scaler = nodeObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2560, 1600);
            scaler.matchWidthOrHeight = 0.5f;

            nodeObject.AddComponent<GraphicRaycaster>();
            nodeObject.transform.SetParent(_canvasRoot, false);
            SortRootChildren(_canvasRoot);
            _canvasNodes[layer] = node = nodeObject.transform;
        }

        tr.SetParent(node, false);
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