using BBJ.EventSystem;
using BBJ.Register;
using BBJ.Schedule;
using BBJ.Work;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Order
{
    public class OrderManager : MonoBehaviour
    {
        public static OrderManager Instance { get; private set; }

        [SerializeField] private OrderRegisterSO  _orderRegister;
        [SerializeField] private ScheduleManager  _scheduleManager;
        [SerializeField] private WorkDispatchTableSO _dispatchTable;
        [SerializeField] private EventChannelSO   _orderChannel;
        public OrderRegisterSO OrderRegister => _orderRegister;

        private void Awake() => Instance = this;

        public void Register(OrderTicket ticket)
        {
            if (_orderRegister == null) return;
            _orderRegister.Register(ticket);
            _orderChannel?.RaiseEvent(new OrderRegisteredEvent(ticket));
            _dispatchTable?.Dispatch(ticket.WorkPhase, new OrderWorkEvent(ticket, this), _scheduleManager);
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
            _dispatchTable?.Dispatch(ticket.WorkPhase, new OrderWorkEvent(ticket, this), _scheduleManager);
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
                _dispatchTable?.Dispatch(ticket.WorkPhase, new OrderWorkEvent(ticket, this), _scheduleManager);
            }
            return true;
        }

        public void CancelOrder(OrderTicket ticket, CancelReason reason)
        {
            if (ticket.State is OrderState.Done or OrderState.Cancelled) return;

            ticket.Cancel(reason);
            _orderRegister.Unregister(ticket);
            _orderChannel?.RaiseEvent(new OrderUnregisteredEvent(ticket));
        }

        private void HandleInterrupted(OrderTicket ticket)
        {
            ticket.Release();
            _orderChannel?.RaiseEvent(new OrderStateChangedEvent(ticket));
            _dispatchTable?.Dispatch(ticket.WorkPhase, new OrderWorkEvent(ticket, this), _scheduleManager);
        }

        private static bool IsOwner(OrderTicket ticket, ModuleOwner actor) =>
            actor != null && ticket.ReservedBy == actor;

        private void OnDestroy()
        {
            if (_orderRegister == null) return;

            foreach (var ticket in _orderRegister.Registry)
                ticket.Cancel(CancelReason.SceneUnloaded);
            _orderRegister.Clear();
        }
    }
}
