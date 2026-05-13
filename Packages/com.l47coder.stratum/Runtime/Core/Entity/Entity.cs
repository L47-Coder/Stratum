using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratum
{
    public sealed class Entity : MonoBehaviour
    {
        internal List<EntityComponentEntry> Components = new();
        internal Dictionary<Type, List<Component>> PoolAll = new();
        internal Dictionary<Type, Stack<Component>> PoolIdle = new();
        internal IEntityHandle EntityHandle; //由对象池写入

        internal event Action<IEntityHandle, Collider> TriggerEnter;
        internal event Action<IEntityHandle, Collider> TriggerExit;
        internal event Action<IEntityHandle, Collider> TriggerStay;
        internal event Action<IEntityHandle, Collision> CollisionEnter;
        internal event Action<IEntityHandle, Collision> CollisionExit;
        internal event Action<IEntityHandle, Collision> CollisionStay;

        internal event Action<IEntityHandle, Collider2D> TriggerEnter2D;
        internal event Action<IEntityHandle, Collider2D> TriggerExit2D;
        internal event Action<IEntityHandle, Collider2D> TriggerStay2D;
        internal event Action<IEntityHandle, Collision2D> CollisionEnter2D;
        internal event Action<IEntityHandle, Collision2D> CollisionExit2D;
        internal event Action<IEntityHandle, Collision2D> CollisionStay2D;

        private void OnTriggerEnter(Collider other) => TriggerEnter?.Invoke(EntityHandle, other);
        private void OnTriggerExit(Collider other) => TriggerExit?.Invoke(EntityHandle, other);
        private void OnTriggerStay(Collider other) => TriggerStay?.Invoke(EntityHandle, other);
        private void OnCollisionEnter(Collision c) => CollisionEnter?.Invoke(EntityHandle, c);
        private void OnCollisionExit(Collision c) => CollisionExit?.Invoke(EntityHandle, c);
        private void OnCollisionStay(Collision c) => CollisionStay?.Invoke(EntityHandle, c);

        private void OnTriggerEnter2D(Collider2D other) => TriggerEnter2D?.Invoke(EntityHandle, other);
        private void OnTriggerExit2D(Collider2D other) => TriggerExit2D?.Invoke(EntityHandle, other);
        private void OnTriggerStay2D(Collider2D other) => TriggerStay2D?.Invoke(EntityHandle, other);
        private void OnCollisionEnter2D(Collision2D c) => CollisionEnter2D?.Invoke(EntityHandle, c);
        private void OnCollisionExit2D(Collision2D c) => CollisionExit2D?.Invoke(EntityHandle, c);
        private void OnCollisionStay2D(Collision2D c) => CollisionStay2D?.Invoke(EntityHandle, c);
    }
}
