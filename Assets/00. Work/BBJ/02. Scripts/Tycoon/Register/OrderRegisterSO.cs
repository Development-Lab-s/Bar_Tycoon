using BBJ.Register;
using System.Linq;
using UnityEngine;

namespace BBJ.Tycoon
{
    [CreateAssetMenu(fileName = "OrderRegister", menuName = "Tycoon/Register/OrderRegister")]
    public class OrderRegisterSO : RegisterSO<OrderTicket>
    {
        public OrderTicket GetOldest(OrderState state)
            => Agents.FirstOrDefault(t => t.State == state);

        public bool HasOrder(OrderState state)
            => Agents.Any(t => t.State == state);
    }
}
