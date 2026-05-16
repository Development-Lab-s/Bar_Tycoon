
using Gamelib.ObjectPool.Runtime;
using UnityEngine;

namespace BBJ.Customer
{
    public abstract class Customer : AbstractTycoonAgent, IPoolable
    {
        [field: SerializeField] public PoolItemSo PoolItem { get; set; }
        public GameObject GameObject => gameObject;

        public virtual void ResetItem() { }
    }
}
