using _00._Work._Resources._02._Scripts.Modules;
using _00._Work.Goat._02._Scripts.Events;
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
        [SerializeField] private CalculatorSO     _calculator; // 계산기

        // 알 수 없음
        [SerializeField] private EventChannelSO   _coinChannel;
        [SerializeField] private EventChannelSO   _particleChannel;
        [SerializeField] private CostParticleType _particleType;

        // 보유자와, 실행자에 대한 정보
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
