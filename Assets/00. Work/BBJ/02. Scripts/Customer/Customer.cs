
using Gamelib.ObjectPool.Runtime;
using UnityEngine;

namespace BBJ.Customer
{
    public abstract class Customer : TycoonAgent, IPoolable
    {
        [field: SerializeField] public PoolItemSo PoolItem { get; set; }
        public GameObject GameObject => gameObject;

        public virtual void ResetItem() { }
    }
}
