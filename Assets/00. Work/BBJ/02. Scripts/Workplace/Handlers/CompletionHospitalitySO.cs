using _00._Work._Resources._02._Scripts.Modules;
using _00._Work.Goat._02._Scripts.Events;
using BBJ.Order;
using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.WorkplaceSystem.Handlers
{
    [CreateAssetMenu(fileName = "CompletionHospitalitySO", menuName = "Tycoon/WorkCompletion/Hospitality")]
    public class CompletionHospitalitySO : WorkCompletionHandlerSO
    {
        [SerializeField] private float _stageMultiplier = 1f;
        [SerializeField] private EventChannelSO _expChannel;

        // 알 수 없음
        //[SerializeField] private CalculatorSO _calculator; // 계산기
        //[SerializeField] private EventChannelSO _particleChannel;
        //[SerializeField] private CostParticleType _particleType;

        // 보유자와, 실행자에 대한 정보
        public override void OnCompleted(ModuleOwner executorStat, OrderTicket orderTicket)
        {
            var food = orderTicket.Ordered;
            if (food == null) return;

            int amount = Mathf.RoundToInt(food.unlockStage * _stageMultiplier);
            _expChannel?.RaiseEvent(new ExpEvent().Init(amount));
            //int tip = _calculator != null ? _calculator.Calculate(executorStat, orderTicket) : 0;

            //_particleChannel?.RaiseEvent(new CostParticleEvent().Init(
            //        _particleType, amount, orderTicket.Customer.transform.position));
        }
    }
}
