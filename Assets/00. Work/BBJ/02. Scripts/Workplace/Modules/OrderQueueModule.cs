using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Order;
using UnityEngine;

namespace BBJ.WorkplaceSystem.Modules
{
    public class OrderQueueModule : MonoBehaviour, IModule
    {
        [SerializeField] private OrderQueueSO _queue;

        public void Initialize(ModuleOwner owner) { }

        public bool HasReadyOrder() => _queue != null && _queue.HasPending;

        public void Enqueue(OrderTicket ticket) => _queue?.Enqueue(ticket);

        public OrderTicket Dequeue()
            => _queue != null && _queue.TryDequeue(out var t) ? t : null;
    }
}
