using BBJ.Order;
using Cysharp.Threading.Tasks;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Customer;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "CustomerCycleSequence", menuName = "Tycoon/Work/CustomerCycleSequence")]
    public class CustomerCycleSequenceSO : WorkSO
    {
        [SerializeField] private WorkSO[] _steps;
        [SerializeField] private Vector2 _stepDelayRange; // x=min, y=max (seconds)

        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
        {
            if (_steps == null) return WorkResult.Completed;

            int startStep = 0;
            if (executor is CustomerAgent ca)
            {
                startStep = ca.RestoreCycleStep;
                ca.RestoreCycleStep = 0;

                // PaymentDone なのにサイクルが最初から再実行された場合、ExitWork へ直行
                if (ca.PaymentDone && startStep == 0)
                {
                    for (int j = _steps.Length - 1; j >= 0; j--)
                    {
                        if (_steps[j] is ExitWorkSO) { startStep = j; break; }
                    }
                }
            }

            for (int i = startStep; i < _steps.Length; i++)
            {
                var step = _steps[i];
                if (step == null) continue;

                await ctx.WaitIfPausedAsync(ctx.Token);

                var stepResult = await step.ExecuteAsync(executor, null, ctx);

                if (stepResult == WorkResult.Cancelled) return WorkResult.Cancelled;

                bool isLast = i == _steps.Length - 1;
                if (!isLast && _stepDelayRange.y > 0f)
                {
                    await ctx.WaitIfPausedAsync(ctx.Token);
                    float delay = Random.Range(_stepDelayRange.x, _stepDelayRange.y);
                    await UniTask.Delay(System.TimeSpan.FromSeconds(delay), cancellationToken: ctx.Token);
                }
            }

            return WorkResult.Completed;
        }
    }
}
