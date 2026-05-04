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

            if (entity.InPool.TryGetValue(type, out var pool) && pool.Count > 0)
                return (T)pool.Pop();

            var fresh = go.AddComponent<T>();

            if (!entity.All.TryGetValue(type, out var all))
                entity.All[type] = all = new List<Component>();
            all.Add(fresh);

            return fresh;
        }

        public static void Release<T>(T component) where T : Component
        {
            if (component == null) return;
            if (!component.gameObject.TryGetComponent<Entity>(out var entity)) return;

            var type = typeof(T);
            if (!entity.InPool.TryGetValue(type, out var pool))
                entity.InPool[type] = pool = new Stack<Component>();
            pool.Push(component);
        }
    }
}
