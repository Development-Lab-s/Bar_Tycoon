using BBJ.EventSystem;
using BBJ.Order;
using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.Scene
{
    public class CocktailTransitionTrigger : MonoBehaviour
    {
        public static CocktailTransitionTrigger Instance { get; private set; }

        [SerializeField] private EventChannelSO _orderChannel;

        private OrderTicket _pendingTicket;
        public OrderTicket PendingTicket => _pendingTicket;

        private void Awake()
        {
            Instance = this;
            UtilDebugger.AssertAllAssigned(this);
            _orderChannel.AddListener<PlayerCraftStartEvent>(OnCraftStart);
        }

        private void OnDestroy()
        {
            Instance = null;
            _orderChannel.RemoveListener<PlayerCraftStartEvent>(OnCraftStart);
        }

        public void Clear() => _pendingTicket = null;

        private void OnCraftStart(PlayerCraftStartEvent e)
        {
            var ticket = e.Ticket;
            if (!ticket.TryStartProgress(ticket.ReservedBy)) return;
            _pendingTicket = ticket;
        }
    }
}
