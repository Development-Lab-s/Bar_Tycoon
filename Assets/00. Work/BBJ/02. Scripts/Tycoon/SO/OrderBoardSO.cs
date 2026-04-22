using BBJ.Register;
using Gamelib.EventSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BBJ.Tycoon
{
    /// <summary>
    /// 주문 흐름 전체를 담당하는 Runtime SO.
    ///
    /// 변경: _pendingCooks를 LinkedList로 교체.
    ///   - 재배정 실패 시 맨 앞 재삽입이 O(1) (기존 Queue 전체 재구성 O(n) 제거).
    /// </summary>
    [CreateAssetMenu(fileName = "OrderBoard", menuName = "Tycoon/SO/OrderBoard")]
    public class OrderBoardSO : ScriptableObject
    {
        [Header("References")]
        [SerializeField] private WorkplaceRegisterSO _workplaceRegister;
        [SerializeField] private EventChannelSO      _scheduleTriggerChannel;

        private readonly List<OrderTicket>       _tickets      = new();
        private readonly LinkedList<OrderTicket> _pendingCooks = new();

        // ─── 주문서 등록/해제 ───────────────────────
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

        // ─── 주문 → CookStation 배정 ────────────────
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
                _pendingCooks.AddFirst(ticket); // O(1) 맨 앞 재삽입
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
            => _scheduleTriggerChannel?.RaiseEvent(new BBJ.Schedule.ScheduleTriggerEvent());

        private void OnDisable()
        {
            _tickets.Clear();
            _pendingCooks.Clear();
            OrderTicket.ResetCounter(); // static 카운터 초기화 — 씬 재로드 시 동기화
        }
    }
}

