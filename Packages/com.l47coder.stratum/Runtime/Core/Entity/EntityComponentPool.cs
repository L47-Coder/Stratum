using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratum
{
    public static class ComponentPool
    {
        public static T Acquire<T>(GameObject go) where T : Component
        {
            if (go == null) throw new ArgumentNullException(nameof(go));

            if (!go.TryGetComponent<Entity>(out var entity))
                entity = go.AddComponent<Entity>();

            var type = typeof(T);

            if (entity.PoolIdle.TryGetValue(type, out var idle) && idle.Count > 0)
                return (T)idle.Pop();

            var fresh = go.AddComponent<T>();

            if (!entity.PoolAll.TryGetValue(type, out var all))
                entity.PoolAll[type] = all = new List<Component>();
            all.Add(fresh);

            return fresh;
        }

        public static void Release<T>(T component) where T : Component
        {
            if (component == null) return;
            if (!component.gameObject.TryGetComponent<Entity>(out var entity)) return;

            var type = typeof(T);
            if (!entity.PoolIdle.TryGetValue(type, out var idle))
                entity.PoolIdle[type] = idle = new Stack<Component>();
            idle.Push(component);
        }
    }
}
