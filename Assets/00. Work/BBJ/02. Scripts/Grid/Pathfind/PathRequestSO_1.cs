using System;
using UnityEngine;

namespace BBJ.GridSystem.Pathfind
{
    public abstract class RuntimeReference<T> : ScriptableObject where T : class
    {
        protected T instance;
        public T Instance
        {
            get
            {
                Debug.Assert(instance != null, $"[{this.GetType().Name}] {nameof(T)}가 주입되지 않았습니다.");
                return instance;
            }

            protected set => instance = value;
        }
        public virtual void Initialize(T newInstance)
        {
            if (null != newInstance)
                instance = newInstance;
        }

        protected virtual void OnDisable() => instance = null;

    }
}