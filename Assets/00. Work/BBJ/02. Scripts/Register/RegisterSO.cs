using BBJ.WorkplaceSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBJ.Register
{
    public abstract class RegisterSO<T> : ScriptableObject
    {
        protected readonly HashSet<T> _registrySet = new();
        public IReadOnlyCollection<T> Registry => _registrySet;
        public void Register(T occupancy)
        {
            if (_registrySet.Add(occupancy))
                Add(occupancy);
        }

        public void Unregister(T release)
        {
            if (_registrySet.Remove(release))
                Remove(release);
        }
        public virtual void Add(T occupancy) { }
        public virtual void Remove(T release) { }
        public virtual void Clear() => _registrySet.Clear();

        protected virtual void OnDisable() => Clear();
    }
}