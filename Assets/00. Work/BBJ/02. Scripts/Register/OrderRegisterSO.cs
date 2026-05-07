using BBJ.Order;
using System.Collections.Generic;
using UnityEngine;

namespace BBJ.Register
{
    [CreateAssetMenu(fileName = "OrderRegister", menuName = "Tycoon/Register/OrderRegister")]
    public class OrderRegisterSO : RegisterSO<OrderTicket>
    {
        private Dictionary<OrderState, Queue<OrderTicket>> _ticketDictionary = new();
        public override void Add(OrderTicket occupancy)
        {
            var key = occupancy.State;

            if (!_ticketDictionary.TryGetValue(key, out var queue))
            {
                queue = new Queue<OrderTicket>();
                _ticketDictionary[key] = queue;
            }

            queue.Enqueue(occupancy);
        }

        public OrderTicket GetOldest(OrderState state)
        {
            if (!_ticketDictionary.TryGetValue(state, out var queue))
                return null;

            while (queue.Count > 0)
            {
                var ticket = queue.Peek(); 
                if (_registrySet.Contains(ticket) && ticket.State == state)
                    return ticket;
                queue.Dequeue();
            }

            return null;
        }

        public bool HasOrder(OrderState state) => GetOldest(state) != null;

        public override void Clear()
        {
            base.Clear();
            _ticketDictionary.Clear();
        }
    }
}