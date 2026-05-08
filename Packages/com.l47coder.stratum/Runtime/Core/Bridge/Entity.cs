using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratum
{
    public interface IOnTriggerEnter { void OnTriggerEntered(Collider other); }
    public interface IOnTriggerExit { void OnTriggerExited(Collider other); }
    public interface IOnTriggerStay { void OnTriggerStayed(Collider other); }

    public interface IOnCollisionEnter { void OnCollisionEntered(Collision c); }
    public interface IOnCollisionExit { void OnCollisionExited(Collision c); }
    public interface IOnCollisionStay { void OnCollisionStayed(Collision c); }

    public interface IOnTriggerEnter2D { void OnTriggerEntered2D(Collider2D other); }
    public interface IOnTriggerExit2D { void OnTriggerExited2D(Collider2D other); }
    public interface IOnTriggerStay2D { void OnTriggerStayed2D(Collider2D other); }

    public interface IOnCollisionEnter2D { void OnCollisionEntered2D(Collision2D c); }
    public interface IOnCollisionExit2D { void OnCollisionExited2D(Collision2D c); }
    public interface IOnCollisionStay2D { void OnCollisionStayed2D(Collision2D c); }

    public sealed class Entity : MonoBehaviour
    {
        public List<EntityComponentEntry> Components = new();

        internal event Action<GameObject, Collider> TriggerEnter;
        internal event Action<GameObject, Collider> TriggerExit;
        internal event Action<GameObject, Collider> TriggerStay;
        internal event Action<GameObject, Collision> CollisionEnter;
        internal event Action<GameObject, Collision> CollisionExit;
        internal event Action<GameObject, Collision> CollisionStay;

        internal event Action<GameObject, Collider2D> TriggerEnter2D;
        internal event Action<GameObject, Collider2D> TriggerExit2D;
        internal event Action<GameObject, Collider2D> TriggerStay2D;
        internal event Action<GameObject, Collision2D> CollisionEnter2D;
        internal event Action<GameObject, Collision2D> CollisionExit2D;
        internal event Action<GameObject, Collision2D> CollisionStay2D;

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

        internal readonly Dictionary<Type, List<Component>> All = new();
        internal readonly Dictionary<Type, Stack<Component>> InPool = new();
    }
}
