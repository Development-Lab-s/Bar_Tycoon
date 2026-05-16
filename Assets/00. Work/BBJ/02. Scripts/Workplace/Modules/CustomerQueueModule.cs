using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Customer;
using BBJ.Schedule;
using Gamelib.EventSystem;
using System.Collections.Generic;
using UnityEngine;

namespace BBJ.WorkplaceSystem.Modules
{
    public class CustomerQueueModule : MonoBehaviour, IModule
    {
        [SerializeField] private EventChannelSO _scheduleChannel;

        private readonly Queue<CustomerAgent> _payingCustomers = new();

        public void Initialize(ModuleOwner owner) { }

        private void OnDisable() => _payingCustomers.Clear();

        public bool HasPayingCustomer() => _payingCustomers.Count > 0;

        public void EnqueuePaying(CustomerAgent customer)
        {
            _payingCustomers.Enqueue(customer);
            _scheduleChannel?.RaiseEvent(new ScheduleTriggerEvent());
        }

        public CustomerAgent DequeuePaying()
            => _payingCustomers.Count > 0 ? _payingCustomers.Dequeue() : null;
    }
}
