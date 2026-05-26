using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Order;
using UnityEngine;

namespace BBJ.WorkplaceSystem.Handlers
{
    public abstract class CalculatorSO : ScriptableObject
    {
        public abstract int Calculate(ModuleOwner executor, OrderTicket orderTicket);
    }
}
