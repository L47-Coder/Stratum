using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratum
{
    [DisallowMultipleComponent]
    public abstract class BaseModule { }

    public interface IEntity
    {
        GameObject GameObject { get; }
        Transform Transform { get; }
        T GetOrAddModule<T>() where T : BaseModule, new();
        bool TryGetModule<T>(out T module) where T : BaseModule;
        bool RemoveModule<T>() where T : BaseModule;
        IReadOnlyCollection<BaseModule> GetAllModules();
        void RemoveAllModules();
    }

    public sealed class Entity : MonoBehaviour, IEntity
    {
        private readonly Dictionary<Type, BaseModule> _modules = new();
        public GameObject GameObject => gameObject;
        public Transform Transform => transform;

        public T GetOrAddModule<T>() where T : BaseModule, new()
        {
            var type = typeof(T);
            if (_modules.TryGetValue(type, out var existing))
            {
                if (existing != null) return (T)existing;
                _modules.Remove(type);
            }
            var added = new T();
            _modules[type] = added;
            return added;
        }

        public bool TryGetModule<T>(out T module) where T : BaseModule
        {
            if (_modules.TryGetValue(typeof(T), out var existing) && existing != null)
            {
                module = (T)existing;
                return true;
            }
            module = null;
            return false;
        }

        public bool RemoveModule<T>() where T : BaseModule
        {
            var type = typeof(T);
            if (!_modules.TryGetValue(type, out var module)) return false;
            _modules.Remove(type);
            if (module != null) Destroy(module);
            return true;
        }

        public IReadOnlyCollection<BaseModule> GetAllModules() => _modules.Values;

        public void RemoveAllModules()
        {
            foreach (var module in _modules.Values)
                if (module != null) Destroy(module);
            _modules.Clear();
        }
    }
}
