using System.Collections.Generic;
using UnityEngine;

namespace BBJ.Order
{
    [CreateAssetMenu(fileName = "OrderQueue", menuName = "Tycoon/Order/OrderQueue")]
    public class OrderQueueSO : ScriptableObject
    {
        private readonly Queue<OrderTicket> _queue = new();

        public bool HasPending => _queue.Count > 0;

        private void OnEnable() => _queue.Clear();

        public void Enqueue(OrderTicket ticket) => _queue.Enqueue(ticket);

        public bool TryDequeue(out OrderTicket ticket)
        {
            if (_queue.Count == 0) { ticket = null; return false; }
            ticket = _queue.Dequeue();
            return true;
        }

        public void Clear() => _queue.Clear();
    }
}
