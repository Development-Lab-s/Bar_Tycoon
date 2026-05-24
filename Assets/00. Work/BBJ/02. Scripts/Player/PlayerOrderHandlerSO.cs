using BBJ.Customer;
using BBJ.EventSystem;
using BBJ.Order;
using BBJ.Work;
using Gamelib.EventSystem;
using System.Collections.Generic;
using UnityEngine;

namespace BBJ.Player
{
    [CreateAssetMenu(fileName = "PlayerOrderHandler", menuName = "Tycoon/Player/OrderHandler")]
    public class PlayerOrderHandlerSO : ScriptableObject
    {
        [SerializeField] private EventChannelSO       _orderChannel;
        [SerializeField] private EventChannelSO       _uiChannel;
        [SerializeField] private WorkContextSO        _workCtx;
        [SerializeField] private List<PlayerActionSO> _actions;

        private PlayerActionContext _context;

        public void RuntimeInit(PlayerOrderHandle player)
        {
            _context = new PlayerActionContext
            {
                Player       = player,
                Register     = _workCtx?.WorkplaceRegister,
                CounterType  = _workCtx?.CounterType,
                OrderChannel = _orderChannel,
            };
        }

        public void OnCustomerClicked(CustomerAgent customer)
        {
            if (customer == null) return;

            var ticket = customer.ActiveTicket;
            if (ticket == null || ticket.IsTerminal) return;

            if (!ticket.IsPlayerActionable)
            {
                _uiChannel?.RaiseEvent(new MessageEvent("지금은 처리할 수 없습니다."));
                return;
            }

            if (_context?.Player?.IsProcessing == true) return;

            var action = _actions?.Find(a => a.CanHandle(ticket));
            action?.Execute(ticket, customer, _context);
        }
    }
}
