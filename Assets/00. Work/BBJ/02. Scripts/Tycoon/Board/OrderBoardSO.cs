using BBJ.Register;
using BBJ.Schedule;
using Gamelib.EventSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BBJ.Tycoon.Board
{
    [CreateAssetMenu(fileName = "OrderBoard", menuName = "Tycoon/SO/OrderBoard")]
    public class OrderBoardSO : ScriptableObject
    {
        [Header("References")]
        [SerializeField] private WorkplaceRegisterSO _workplaceRegister;
        [SerializeField] private EventChannelSO      _scheduleTriggerChannel;

        private readonly List<OrderTicket>       _tickets      = new();
        private readonly LinkedList<OrderTicket> _pendingCooks = new();

        public void Register(OrderTicket ticket)
        {
            if (!_tickets.Contains(ticket))
                _tickets.Add(ticket);
        }

        public void Unregister(OrderTicket ticket) => _tickets.Remove(ticket);

        public OrderTicket GetOldest(OrderState state)
            => _tickets.FirstOrDefault(t => t.State == state);

        public bool HasOrder(OrderState state)
            => _tickets.Any(t => t.State == state);

        public void AssignOrderToCookStation(OrderTicket ticket)
        {
            ticket.ChangeState(OrderState.Ordered);

            if (!TryAssignToCookStation(ticket))
                _pendingCooks.AddLast(ticket);
        }

        public void OnCookStationReleased()
        {
            if (_pendingCooks.Count == 0) return;

            var ticket = _pendingCooks.First.Value;
            _pendingCooks.RemoveFirst();

            if (!TryAssignToCookStation(ticket))
                _pendingCooks.AddFirst(ticket);
        }

        private bool TryAssignToCookStation(OrderTicket ticket)
        {
            var stations = _workplaceRegister.GetAll<CookStationWorkplace>(WorkplaceType.CookStation);
            foreach (var station in stations)
            {
                if (station.IsOccupied) continue;
                station.AssignTicket(ticket);
                TriggerSchedule();
                return true;
            }
            return false;
        }

        public void TriggerSchedule()
            => _scheduleTriggerChannel?.RaiseEvent(new ScheduleTriggerEvent());

        private void OnDisable()
        {
            _tickets.Clear();
            _pendingCooks.Clear();
            OrderTicket.ResetCounter();
        }
    }
}
