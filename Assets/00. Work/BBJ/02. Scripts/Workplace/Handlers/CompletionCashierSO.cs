using _00._Work._Resources._02._Scripts.Modules;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Lusaload._02._Scripts.SO;
using Agents.StatSystem;
using BBJ.Order;
using BBJ.Particle;
using BBJ.Work;
using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.WorkplaceSystem.Handlers
{
    [CreateAssetMenu(fileName = "CompletionCashierSO", menuName = "Tycoon/WorkCompletion/Cashier")]
    public class CompletionCashierSO : WorkCompletionHandlerSO
    {
        [SerializeField] private float            _stageMultiplier = 1f;
        [SerializeField] private CalculatorSO     _calculator; // ����

        // �� �� ����
        [SerializeField] private EventChannelSO   _coinChannel;
        [SerializeField] private EventChannelSO   _particleChannel;
        [SerializeField] private CostParticleType _particleType;

        public int CalculateBase(CocktailRecipeSO food)
        {
            if (food == null) return 0;
            return Mathf.RoundToInt(food.unlockStage * _stageMultiplier);
        }

        // �����ڿ�, �����ڿ� ���� ����
        public override void OnCompleted(ModuleOwner executorStat, OrderTicket orderTicket)
        {
            var food = orderTicket.Ordered;
            if (food == null) return;

            int tip    = _calculator != null ? _calculator.Calculate(executorStat, orderTicket) : 0;
            int amount = Mathf.RoundToInt(food.unlockStage * _stageMultiplier) + tip;

            _coinChannel?.RaiseEvent(new CoinEvent().Init(amount));
            _particleChannel?.RaiseEvent( new CostParticleEvent().Init(
                    _particleType, amount, orderTicket.Customer.transform.position));
        }
    }
}
