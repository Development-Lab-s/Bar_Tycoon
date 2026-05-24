using BBJ.Customer;
using BBJ.Order;
using UnityEngine;

namespace BBJ.Player
{
    public abstract class PlayerActionSO : ScriptableObject
    {
        [SerializeField] private OrderWorkPhase _targetPhase;

        public bool CanHandle(OrderTicket ticket) => ticket.WorkPhase == _targetPhase;

        public abstract void Execute(OrderTicket ticket, CustomerAgent customer, PlayerActionContext context);
    }
}
