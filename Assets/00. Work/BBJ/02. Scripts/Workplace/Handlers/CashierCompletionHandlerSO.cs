using _00._Work.Goat._02._Scripts.Events;
using BBJ.Particle;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Work;

namespace BBJ.WorkplaceSystem.Handlers
{
    [CreateAssetMenu(fileName = "CashierCompletion", menuName = "Tycoon/WorkCompletion/Cashier")]
    public class CashierCompletionHandlerSO : WorkCompletionHandlerSO
    {
        [SerializeField] private float            _stageMultiplier = 1f;
        [SerializeField] private TipCalculatorSO  _tipCalculator;
        [SerializeField] private EventChannelSO   _coinChannel;
        [SerializeField] private EventChannelSO   _particleChannel;
        [SerializeField] private CostParticleType _particleType;

        public override void OnCompleted(ModuleOwner executor, Vector3 position)
        {
            var food = executor.GetModule<ICurrentFoodProvider>()?.CurrentFood;
            if (food == null) return;

            int tip    = _tipCalculator != null ? _tipCalculator.Calculate(executor) : 0;
            int amount = Mathf.RoundToInt(food.price * _stageMultiplier) + tip;

            Debug.Log("계산 완료는 들어옴");
            _coinChannel?.RaiseEvent(new CoinEvent().Init(amount));
            _particleChannel?.RaiseEvent(
                new CostParticleEvent().Init(_particleType, amount, position));
        }
    }
}
