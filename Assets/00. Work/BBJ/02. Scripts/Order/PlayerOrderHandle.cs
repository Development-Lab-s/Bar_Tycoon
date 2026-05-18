using _00._Work.Lusaload._02._Scripts.SO;
using BBJ.EventSystem;
using Gamelib.EventSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Order
{
    public class PlayerOrderHandle : ModuleOwner
    {
        [SerializeField] private EventChannelSO _orderChannel;

        private readonly Dictionary<CocktailRecipeSO, int>               _readyCount     = new();
        private readonly Dictionary<CocktailRecipeSO, List<OrderTicket>> _pendingTickets = new();

        protected override void Awake()
        {
            base.Awake();

            UtilDebugger.AssertAllAssigned(this);
        }

        private void OnEnable()
        {
            _orderChannel.AddListener<OrderRegisteredEvent>(OnOrderRegistered);
            _orderChannel.AddListener<OrderUnregisteredEvent>(OnOrderUnregistered);
            _orderChannel.AddListener<OrderStateChangedEvent>(OnOrderStateChanged);
        }

        private void OnDisable()
        {
            _orderChannel.RemoveListener<OrderRegisteredEvent>(OnOrderRegistered);
            _orderChannel.RemoveListener<OrderUnregisteredEvent>(OnOrderUnregistered);
            _orderChannel.RemoveListener<OrderStateChangedEvent>(OnOrderStateChanged);
        }

        // --- 쿼리 ---

        public OrderTicket GetFreeTicket(CocktailRecipeSO food)
        {
            if (!_pendingTickets.TryGetValue(food, out var list)) return null;
            return list.FirstOrDefault(t => t.State == OrderState.Waiting);
        }

        public OrderTicket GetOccupiedTicket(CocktailRecipeSO food)
        {
            if (!_pendingTickets.TryGetValue(food, out var list)) return null;
            return list.FirstOrDefault(t => t.State is OrderState.Reserved or OrderState.InProgress);
        }

        public bool HasFreeTicket(CocktailRecipeSO food)     => GetFreeTicket(food) != null;
        public bool HasOccupiedTicket(CocktailRecipeSO food) => GetOccupiedTicket(food) != null;

        public IReadOnlyList<OrderTicket> GetPendingTickets(CocktailRecipeSO food)
        {
            if (!_pendingTickets.TryGetValue(food, out var list))
                return System.Array.Empty<OrderTicket>();
            return list;
        }

        public IEnumerable<CocktailRecipeSO> GetAllPendingFoods() => _pendingTickets.Keys.ToList();

        // --- 액션 ---

        public bool TryOccupy(OrderTicket ticket)
        {
            if (!ticket.TryReserve(this)) return false;
            _orderChannel?.RaiseEvent(new OrderStateChangedEvent(ticket));
            return true;
        }

        public bool TrySteal(OrderTicket ticket)
        {
            if (ticket.WorkPhase != OrderWorkPhase.PendingCook) return false;
            if (!ticket.TrySteal(this)) return false;
            _orderChannel?.RaiseEvent(new OrderStateChangedEvent(ticket));
            return true;
        }

        public void AddReadyFood(CocktailRecipeSO food)
        {
            if (food == null) return;
            _readyCount.TryGetValue(food, out int count);
            _readyCount[food] = count + 1;
        }

        public bool ConsumeReadyFood(CocktailRecipeSO food)
        {
            if (food == null || !_readyCount.TryGetValue(food, out int count) || count <= 0) return false;
            _readyCount[food] = count - 1;
            return true;
        }

        public int GetReadyCount(CocktailRecipeSO food)
        {
            _readyCount.TryGetValue(food, out int count);
            return count;
        }

        // --- 이벤트 핸들러 ---

        private void OnOrderRegistered(OrderRegisteredEvent e)
        {
            if (e.Ticket.WorkPhase != OrderWorkPhase.PendingCook) return;
            AddToPending(e.Ticket);
        }

        private void OnOrderUnregistered(OrderUnregisteredEvent e)
        {
            RemoveFromPending(e.Ticket);
        }

        private void OnOrderStateChanged(OrderStateChangedEvent e)
        {
            if (e.Ticket.WorkPhase != OrderWorkPhase.PendingCook)
                RemoveFromPending(e.Ticket);
            else
                AddToPending(e.Ticket);
        }

        private void AddToPending(OrderTicket ticket)
        {
            var food = ticket.Ordered;
            if (!_pendingTickets.TryGetValue(food, out var list))
            {
                list = new List<OrderTicket>();
                _pendingTickets[food] = list;
            }
            if (!list.Contains(ticket))
                list.Add(ticket);
        }

        private void RemoveFromPending(OrderTicket ticket)
        {
            var food = ticket.Ordered;
            if (!_pendingTickets.TryGetValue(food, out var list)) return;
            list.Remove(ticket);
            if (list.Count == 0)
                _pendingTickets.Remove(food);
        }
    }
}
