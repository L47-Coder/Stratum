using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratum
{
    internal sealed class Entity : MonoBehaviour
    {
        public List<EntityComponentEntry> Components = new();
        public Dictionary<Type, List<Component>> PoolAll = new();
        public Dictionary<Type, Stack<Component>> PoolIdle = new();

        public event Action<GameObject, Collider> TriggerEnter;
        public event Action<GameObject, Collider> TriggerExit;
        public event Action<GameObject, Collider> TriggerStay;
        public event Action<GameObject, Collision> CollisionEnter;
        public event Action<GameObject, Collision> CollisionExit;
        public event Action<GameObject, Collision> CollisionStay;

        public event Action<GameObject, Collider2D> TriggerEnter2D;
        public event Action<GameObject, Collider2D> TriggerExit2D;
        public event Action<GameObject, Collider2D> TriggerStay2D;
        public event Action<GameObject, Collision2D> CollisionEnter2D;
        public event Action<GameObject, Collision2D> CollisionExit2D;
        public event Action<GameObject, Collision2D> CollisionStay2D;

        private void OnTriggerEnter(Collider other) => TriggerEnter?.Invoke(gameObject, other);
        private void OnTriggerExit(Collider other) => TriggerExit?.Invoke(gameObject, other);
        private void OnTriggerStay(Collider other) => TriggerStay?.Invoke(gameObject, other);
        private void OnCollisionEnter(Collision c) => CollisionEnter?.Invoke(gameObject, c);
        private void OnCollisionExit(Collision c) => CollisionExit?.Invoke(gameObject, c);
        private void OnCollisionStay(Collision c) => CollisionStay?.Invoke(gameObject, c);

        private void OnTriggerEnter2D(Collider2D other) => TriggerEnter2D?.Invoke(gameObject, other);
        private void OnTriggerExit2D(Collider2D other) => TriggerExit2D?.Invoke(gameObject, other);
        private void OnTriggerStay2D(Collider2D other) => TriggerStay2D?.Invoke(gameObject, other);
        private void OnCollisionEnter2D(Collision2D c) => CollisionEnter2D?.Invoke(gameObject, c);
        private void OnCollisionExit2D(Collision2D c) => CollisionExit2D?.Invoke(gameObject, c);
        private void OnCollisionStay2D(Collision2D c) => CollisionStay2D?.Invoke(gameObject, c);
    }
}
