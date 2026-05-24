using BBJ.Customer;
using BBJ.EventSystem;
using BBJ.Register;
using BBJ.Save;
using BBJ.Work;
using Gamelib.EventSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using _00._Work.Lusaload._02._Scripts.SO;

namespace BBJ.Order
{
    public class OrderManager : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private WorkDispatchTableSO _dispatchTable;
        [SerializeField] private OrderRegisterSO     _orderRegister;
        [SerializeField] private EventChannelSO      _orderChannel;

        private void Awake()
        {
            UtilDebugger.AssertAllAssigned(this);

            SubEventChannel();
        }

        private void OnDestroy()
        {
            UnsubEventChannel();
            RegisterClear();
        }

        public void Register(OrderTicket ticket)
        {
            _orderRegister.Register(ticket);
            _orderChannel.RaiseEvent(new OrderRegisteredEvent(ticket));
            _dispatchTable.Dispatch(ticket.WorkPhase, ticket);
        }

        public bool NotifyComplete(OrderTicket ticket, ModuleOwner actor)
        {
            if (!IsOwner(ticket, actor)) return false;
            if (ticket.State != OrderState.InProgress) return false;

            var entry = _dispatchTable?.FindEntry(ticket.WorkPhase);
            if (entry == null || entry.Value.NextPhase == OrderWorkPhase.Done)
            {
                ticket.WorkPhase = OrderWorkPhase.Done;
                ticket.Finish();
                _orderRegister.Unregister(ticket);
                _orderChannel?.RaiseEvent(new OrderUnregisteredEvent(ticket));
                return true;
            }

            ticket.WorkPhase = entry.Value.NextPhase;
            ticket.Release();
            _orderChannel?.RaiseEvent(new OrderStateChangedEvent(ticket));
            _dispatchTable?.Dispatch(ticket.WorkPhase, ticket);
            return true;
        }

        public bool NotifyReleased(OrderTicket ticket, ModuleOwner actor)
        {
            if (!IsOwner(ticket, actor)) return false;
            if (ticket.State is OrderState.Done or OrderState.Cancelled) return false;

            if (ticket.State == OrderState.InProgress)
                HandleInterrupted(ticket);
            else
            {
                ticket.Release();
                _dispatchTable?.Dispatch(ticket.WorkPhase, ticket);
            }
            return true;
        }

        public void CancelOrder(OrderTicket ticket, CancelReason reason)
        {
            if (ticket.IsTerminal) return;

            ticket.Cancel(reason);
            _orderRegister.Unregister(ticket);
            _orderChannel?.RaiseEvent(new OrderUnregisteredEvent(ticket));
        }

        private void HandleInterrupted(OrderTicket ticket)
        {
            ticket.Release();
            _orderChannel?.RaiseEvent(new OrderStateChangedEvent(ticket));
            _dispatchTable?.Dispatch(ticket.WorkPhase, ticket);
        }

        private bool IsOwner(OrderTicket ticket, ModuleOwner actor) =>
            actor != null && ticket.ReservedBy == actor;

        private void SubEventChannel()
        {
            if (_orderChannel == null) return;
            _orderChannel.AddListener<OrderTicketRegisterEvent>(HandleOrderRegisterRequested);
            _orderChannel.AddListener<OrderCancelRequestEvent>(HandleOrderCancelRequested);
            _orderChannel.AddListener<OrderNotifyCompleteEvent>(HandleOrderNotifyComplete);
            _orderChannel.AddListener<OrderNotifyReleasedEvent>(HandleOrderNotifyReleased);
            _orderChannel.AddListener<OrderReadyForServerEvent>(HandleReadyForServer);
            _orderChannel.AddListener<CustomerEatCompleteEvent>(HandleCustomerEatComplete);
        }
        private void UnsubEventChannel()
        {
            if (_orderChannel == null) return;
            _orderChannel.RemoveListener<OrderTicketRegisterEvent>(HandleOrderRegisterRequested);
            _orderChannel.RemoveListener<OrderCancelRequestEvent>(HandleOrderCancelRequested);
            _orderChannel.RemoveListener<OrderNotifyCompleteEvent>(HandleOrderNotifyComplete);
            _orderChannel.RemoveListener<OrderNotifyReleasedEvent>(HandleOrderNotifyReleased);
            _orderChannel.RemoveListener<OrderReadyForServerEvent>(HandleReadyForServer);
            _orderChannel.RemoveListener<CustomerEatCompleteEvent>(HandleCustomerEatComplete);
        }
        private void RegisterClear()
        {
            if (_orderRegister == null) return;

            foreach (var ticket in _orderRegister.Registry)
                ticket.Cancel(CancelReason.SceneUnloaded);
            _orderRegister.Clear();
        }
        private void HandleOrderRegisterRequested(OrderTicketRegisterEvent e) => Register(e.Ticket);
        private void HandleOrderCancelRequested(OrderCancelRequestEvent e) => CancelOrder(e.Ticket, e.Reason);
        private void HandleOrderNotifyComplete(OrderNotifyCompleteEvent e) => NotifyComplete(e.Ticket, e.Actor);
        private void HandleOrderNotifyReleased(OrderNotifyReleasedEvent e) => NotifyReleased(e.Ticket, e.Actor);
        private void HandleReadyForServer(OrderReadyForServerEvent e) => Register(e.Ticket);

        private void HandleCustomerEatComplete(CustomerEatCompleteEvent e)
        {
            var ticket = e.Ticket;
            if (ticket == null || ticket.IsTerminal) return;
            if (ticket.WorkPhase != OrderWorkPhase.Eating) return;

            ticket.WorkPhase = OrderWorkPhase.ReadyForCashier;
            _orderChannel?.RaiseEvent(new OrderStateChangedEvent(ticket));
            _dispatchTable?.Dispatch(ticket.WorkPhase, ticket);
        }

        // ─── 복원 API ───────────────────────────────────

        public OrdersSaveData GetOrdersSaveData()
        {
            var data = new OrdersSaveData();
            foreach (var ticket in _orderRegister.Registry)
            {
                if (ticket.IsTerminal) continue;
                var id = ticket.Ordered?.name;
                if (string.IsNullOrEmpty(id)) continue;
                data.Tickets.Add(new OrderTicketSaveData
                {
                    RecipeId  = id,
                    WorkPhase = ticket.WorkPhase,
                });
            }
            return data;
        }

        public List<OrderTicket> RestoreTickets(OrdersSaveData data, CocktailRecipeDatabaseSO database)
        {
            var restored = new List<OrderTicket>(data.Tickets.Count);
            foreach (var save in data.Tickets)
            {
                var recipe = database?.recipes.FirstOrDefault(r => r.name == save.RecipeId);
                if (recipe == null) continue;

                var ticket = new OrderTicket(recipe, null, null);
                ticket.WorkPhase = save.WorkPhase;
                _orderRegister.Register(ticket);
                restored.Add(ticket);
            }
            return restored;
        }

        public void LinkTicketsToCustomers(List<OrderTicket> tickets, List<CustomerAgent> customers)
        {
            int count = Mathf.Min(tickets.Count, customers.Count);
            for (int i = 0; i < count; i++)
            {
                var ticket   = tickets[i];
                var customer = customers[i];
                var seat     = customer.AssignedSeat;

                ticket.Customer = customer;
                ticket.Seat     = seat;

                customer.RestoreActiveTicket(ticket);

                _orderChannel?.RaiseEvent(new OrderStateChangedEvent(ticket));
                _dispatchTable?.Dispatch(ticket.WorkPhase, ticket);
            }
        }

    }
}
