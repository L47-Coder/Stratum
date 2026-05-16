using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Stratum;
using UnityEngine;

public interface ITaskHandle { }

public interface ITaskBuilder
{
    public ITaskBuilder SetLife(Func<bool> lifeCondition = null, int lifeTime = 0, int lifeFrames = 0);
    public ITaskBuilder Loop(int iterationCount = 0);
    public ITaskBuilder End();
    public ITaskBuilder Action(Action action);
    public ITaskBuilder ActionAsync(Func<CancellationToken, UniTask> asyncFunc);
    public ITaskBuilder WaitCondition(Func<bool> condition);
    public ITaskBuilder WaitTime(int milliseconds);
    public ITaskBuilder WaitFrames(int frames);
    public ITaskHandle Run();
}

public interface ITaskManager
{
    public ITaskBuilder CreateTask();
    public void StopTask(ITaskHandle handle);
    public void StopAllTasks();
}

internal sealed class TaskHandle : ITaskHandle
{
    public string Key { get; }
    public TaskHandle(string key) => Key = key;
}

internal sealed class TaskBuilder : ITaskBuilder
{
    private readonly TaskManager _taskManager;
    private readonly TaskHandle _taskHandle;
    private readonly CancellationToken _cancellationToken;
    private TaskNode _rootNode;
    private TaskNode _currentNode;

    public TaskBuilder(TaskManager taskManager, TaskHandle taskHandle, CancellationToken cancellationToken)
    {
        _taskManager = taskManager;
        _taskHandle = taskHandle;
        _cancellationToken = cancellationToken;
        _currentNode = _rootNode = new RootTaskNode();
    }

    private abstract class TaskNode
    {
        public TaskNode Parent { get; set; }
        public List<TaskNode> ChildNodes { get; } = new();
        public abstract bool ExecuteFrame();
        public abstract void ResetState();
    }

    private sealed class RootTaskNode : TaskNode
    {
        private int _currentChildIndex;

        public override bool ExecuteFrame()
        {
            while (_currentChildIndex < ChildNodes.Count)
            {
                if (!ChildNodes[_currentChildIndex].ExecuteFrame()) return false;
                _currentChildIndex++;
            }

            ResetState();
            return true;
        }

        public override void ResetState()
        {
            _currentChildIndex = 0;
            foreach (var child in ChildNodes)
                child.ResetState();
        }
    }

    private sealed class LoopTaskNode : TaskNode
    {
        private int _targetIterationCount;
        private int _completedIterationCount;
        private int _currentChildIndex;

        public LoopTaskNode(TaskNode parent, int iterationCount)
        {
            Parent = parent;
            _targetIterationCount = iterationCount;
        }

        public override bool ExecuteFrame()
        {
            if (_targetIterationCount <= 0) _targetIterationCount = int.MaxValue;
            if (_completedIterationCount >= _targetIterationCount)
            {
                ResetState();
                return true;
            }

            while (_currentChildIndex < ChildNodes.Count)
            {
                if (!ChildNodes[_currentChildIndex].ExecuteFrame()) return false;
                _currentChildIndex++;
            }

            _currentChildIndex = 0;
            _completedIterationCount++;
            return false;
        }

        public override void ResetState()
        {
            _completedIterationCount = 0;
            _currentChildIndex = 0;
            foreach (var child in ChildNodes)
                child.ResetState();
        }
    }

    private sealed class ActionTaskNode : TaskNode
    {
        private readonly Action _action;

        public ActionTaskNode(Action action) => _action = action ?? (() => { });

        public override bool ExecuteFrame()
        {
            _action();
            return true;
        }

        public override void ResetState() { }
    }

    private sealed class AsyncActionTaskNode : TaskNode
    {
        private readonly Func<CancellationToken, UniTask> _asyncFunc;
        private readonly CancellationToken _cancellationToken;
        private bool _isStarted;
        private bool _isCompleted;
        private CancellationTokenSource _cancellationTokenSource;

        public AsyncActionTaskNode(Func<CancellationToken, UniTask> asyncFunc, CancellationToken cancellationToken)
        {
            _asyncFunc = asyncFunc ?? (async _ => await UniTask.CompletedTask);
            _cancellationToken = cancellationToken;
        }

        public override bool ExecuteFrame()
        {
            if (!_isStarted)
            {
                _isStarted = true;
                _isCompleted = false;
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
                UniTask.Void(async () =>
                {
                    try { await _asyncFunc(_cancellationTokenSource.Token); }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Debug.LogError($"AsyncActionTaskNode 执行出错: {ex.Message}"); }
                    finally { _isCompleted = true; }
                });
            }

            if (_isCompleted)
            {
                ResetState();
                return true;
            }

            return false;
        }

        public override void ResetState()
        {
            if (_cancellationTokenSource == null) return;
            if (!_cancellationTokenSource.IsCancellationRequested)
                _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            _isStarted = false;
            _isCompleted = false;
        }
    }

    private sealed class WaitConditionTaskNode : TaskNode
    {
        private readonly Func<bool> _conditionPredicate;

        public WaitConditionTaskNode(Func<bool> conditionPredicate) => _conditionPredicate = conditionPredicate ?? (() => true);

        public override bool ExecuteFrame() => _conditionPredicate();

        public override void ResetState() { }
    }

    private sealed class WaitTimeTaskNode : TaskNode
    {
        private bool _isStarted;
        private float _waitStartTime;
        private readonly float _waitDurationSeconds;

        public WaitTimeTaskNode(int millisecondsToWait) => _waitDurationSeconds = millisecondsToWait / 1000f;

        public override bool ExecuteFrame()
        {
            if (!_isStarted)
            {
                _isStarted = true;
                _waitStartTime = Time.time;
            }

            if (Time.time - _waitStartTime >= _waitDurationSeconds)
            {
                ResetState();
                return true;
            }

            return false;
        }

        public override void ResetState() => _isStarted = false;
    }

    private sealed class WaitFramesTaskNode : TaskNode
    {
        private bool _isStarted;
        private int _waitStartFrame;
        private readonly int _framesToWait;

        public WaitFramesTaskNode(int framesToWait) => _framesToWait = framesToWait;

        public override bool ExecuteFrame()
        {
            if (!_isStarted)
            {
                _isStarted = true;
                _waitStartFrame = Time.frameCount;
            }

            if (Time.frameCount - _waitStartFrame >= _framesToWait)
            {
                ResetState();
                return true;
            }

            return false;
        }

        public override void ResetState() => _isStarted = false;
    }

    private sealed class MonitorLifeTaskNode : TaskNode
    {
        private readonly TaskNode _monitoredNode;
        private readonly Func<bool> _lifeCondition;
        private readonly float _maxLifetimeSeconds;
        private readonly int _maxLifetimeFrames;
        private bool _monitoringStarted;
        private float _monitoringStartTime;
        private int _monitoringStartFrame;

        public MonitorLifeTaskNode(TaskNode monitoredNode, Func<bool> lifeCondition, int lifetimeMilliseconds, int lifetimeFrames)
        {
            _monitoredNode = monitoredNode;
            _lifeCondition = lifeCondition ?? (() => true);
            _maxLifetimeSeconds = lifetimeMilliseconds > 0 ? lifetimeMilliseconds * 0.001f : float.MaxValue;
            _maxLifetimeFrames = lifetimeFrames > 0 ? lifetimeFrames : int.MaxValue;
        }

        public override bool ExecuteFrame()
        {
            if (!_monitoringStarted)
            {
                _monitoringStarted = true;
                _monitoringStartTime = Time.time;
                _monitoringStartFrame = Time.frameCount;
                if (ChildNodes.Count > 0)
                {
                    _monitoredNode.ChildNodes.AddRange(ChildNodes);
                    ChildNodes.Clear();
                }
            }

            if (!_lifeCondition()
                || Time.time - _monitoringStartTime >= _maxLifetimeSeconds
                || Time.frameCount - _monitoringStartFrame >= _maxLifetimeFrames
                || _monitoredNode.ExecuteFrame())
            {
                ResetState();
                return true;
            }

            return false;
        }

        public override void ResetState()
        {
            _monitoringStarted = false;
            _monitoredNode.ResetState();
        }
    }

    public ITaskBuilder SetLife(Func<bool> lifeCondition = null, int lifeTime = 0, int lifeFrames = 0)
    {
        var lifecycleNode = new MonitorLifeTaskNode(_currentNode, lifeCondition, lifeTime, lifeFrames);
        if (_currentNode.Parent != null)
        {
            lifecycleNode.Parent = _currentNode.Parent;
            _currentNode.Parent.ChildNodes.Remove(_currentNode);
            _currentNode.Parent.ChildNodes.Add(lifecycleNode);
        }
        else
        {
            _rootNode = lifecycleNode;
        }

        _currentNode = lifecycleNode;
        return this;
    }

    public ITaskBuilder Loop(int iterationCount = 0)
    {
        var loopNode = new LoopTaskNode(_currentNode, iterationCount);
        _currentNode.ChildNodes.Add(loopNode);
        _currentNode = loopNode;
        return this;
    }

    public ITaskBuilder End()
    {
        if (_currentNode.Parent != null)
            _currentNode = _currentNode.Parent;
        return this;
    }

    public ITaskBuilder Action(Action action)
    {
        _currentNode.ChildNodes.Add(new ActionTaskNode(action));
        return this;
    }

    public ITaskBuilder ActionAsync(Func<CancellationToken, UniTask> asyncFunc)
    {
        _currentNode.ChildNodes.Add(new AsyncActionTaskNode(asyncFunc, _cancellationToken));
        return this;
    }

    public ITaskBuilder WaitCondition(Func<bool> condition)
    {
        _currentNode.ChildNodes.Add(new WaitConditionTaskNode(condition));
        return this;
    }

    public ITaskBuilder WaitTime(int milliseconds)
    {
        _currentNode.ChildNodes.Add(new WaitTimeTaskNode(milliseconds));
        return this;
    }

    public ITaskBuilder WaitFrames(int frames)
    {
        _currentNode.ChildNodes.Add(new WaitFramesTaskNode(frames));
        return this;
    }

    public ITaskHandle Run()
    {
        UniTask.Void(async () =>
        {
            try
            {
                while (!_cancellationToken.IsCancellationRequested && !_rootNode.ExecuteFrame())
                    await UniTask.NextFrame(_cancellationToken);
            }
            catch (OperationCanceledException) { }
            finally { _taskManager.StopTask(_taskHandle); }
        });

        return _taskHandle;
    }
}

internal sealed partial class TaskManager : ITaskManager
{
    private readonly Dictionary<string, CancellationTokenSource> _taskTokenSources = new(StringComparer.Ordinal);

    public ITaskBuilder CreateTask()
    {
        var key = Guid.NewGuid().ToString("N");
        var cancellationTokenSource = new CancellationTokenSource();
        var taskHandle = new TaskHandle(key);

        _taskTokenSources.Add(taskHandle.Key, cancellationTokenSource);
        return new TaskBuilder(this, taskHandle, cancellationTokenSource.Token);
    }

    public void StopTask(ITaskHandle handle)
    {
        if (handle is not TaskHandle taskHandle) return;
        if (!_taskTokenSources.TryGetValue(taskHandle.Key, out var tokenSource)) return;

        tokenSource.Cancel();
        tokenSource.Dispose();
        _taskTokenSources.Remove(taskHandle.Key);
    }

    public void StopAllTasks()
    {
        foreach (var tokenSource in _taskTokenSources.Values)
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
        }

        _taskTokenSources.Clear();
    }
}
