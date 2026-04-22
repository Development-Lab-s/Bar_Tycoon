using System;
using System.Collections.Generic;
using UnityEngine;

namespace BBJ.Tycoon
{
    public class CounterWorkplace : Workplace
    {
        public event Action OnReadyOrderAdded;
        public event Action OnPayingCustomerAdded;

        private readonly Queue<OrderTicket>   _readyOrders     = new();
        private readonly Queue<CustomerAgent> _payingCustomers = new();

        public bool HasReadyOrder()     => _readyOrders.Count > 0;
        public bool HasPayingCustomer() => _payingCustomers.Count > 0;

        public void AddReadyOrder(OrderTicket ticket)
        {
            _readyOrders.Enqueue(ticket);
            OnReadyOrderAdded?.Invoke();
        }

        public OrderTicket PickupOrder()
            => _readyOrders.Count > 0 ? _readyOrders.Dequeue() : null;

        public void EnqueuePaying(CustomerAgent customer)
        {
            _payingCustomers.Enqueue(customer);
            OnPayingCustomerAdded?.Invoke();
        }

        public CustomerAgent DequeuePayingCustomer()
            => _payingCustomers.Count > 0 ? _payingCustomers.Dequeue() : null;
    }
}
