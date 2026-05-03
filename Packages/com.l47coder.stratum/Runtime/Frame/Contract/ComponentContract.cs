using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Stratum
{
    public abstract class BaseComponentConfig : ScriptableObject
    {
        public abstract Type ConfigItemType { get; }
        public abstract IList GetConfigList();

        protected abstract Dictionary<string, BaseComponentData> GetComponentDataDict();
        internal Dictionary<string, BaseComponentData> ExportComponentDataDict() => GetComponentDataDict();
    }

    public abstract class BaseComponentData
    {
        protected abstract BaseComponent CreateComponent();
        internal BaseComponent InternalCreateComponent() => CreateComponent();
    }

    public abstract class BaseComponent
    {
        public GameObject GameObject { get; private set; }
        public bool IsEnabled { get; private set; }
        
        protected virtual void OnAdd() { }
        protected virtual void OnEnable() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnDisable() { }
        protected virtual void OnRemove() { }

        internal void InternalSetGameObject(GameObject gameObject) => GameObject = gameObject;
        internal void InternalOnAdd() => OnAdd();
        internal void InternalSetEnabled(bool enabled)
        {
            if (IsEnabled == enabled) return;
            IsEnabled = enabled;
            if (enabled) OnEnable();
            else OnDisable();
        }
        internal void InternalOnUpdate()
        {
            if (!IsEnabled) return;
            OnUpdate();
        }
        internal void InternalOnRemove()
        {
            InternalSetEnabled(false);
            OnRemove();
        }
    }
}
