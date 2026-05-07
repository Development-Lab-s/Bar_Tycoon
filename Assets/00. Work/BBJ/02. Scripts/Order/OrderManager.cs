using System.Collections.Generic;
using UnityEngine;

namespace BBJ.Order
{
    public class OrderManager : MonoBehaviour
    {
        public static OrderManager Instance { get; private set; }
        private void Awake() => Instance = this;

        private readonly List<OrderTicket> _activeOrders = new();

        public IReadOnlyList<OrderTicket> ActiveOrders => _activeOrders;

        public void Register(OrderTicket ticket)
        {
            if (ticket == null || _activeOrders.Contains(ticket)) return;
            _activeOrders.Add(ticket);
            ticket.OnStateChanged += OnTicketStateChanged;
        }

        private void OnTicketStateChanged(OrderTicket ticket, OrderState state)
        {
            if (state == OrderState.Done)
            {
                _activeOrders.Remove(ticket);
                ticket.OnStateChanged -= OnTicketStateChanged;
            }
        }
    }
}
