using BBJ.Order;
using Cysharp.Threading.Tasks;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "CustomerCycleSequence", menuName = "Tycoon/Work/CustomerCycleSequence")]
    public class CustomerCycleSequenceSO : WorkSO
    {
        [SerializeField] private WorkSO[] _steps;

        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
        {
            if (_steps == null) return WorkResult.Completed;

            foreach (var step in _steps)
            {
                if (step == null) continue;

                await ctx.WaitIfPausedAsync(ctx.Token);

                var stepResult = await step.ExecuteAsync(executor, null, ctx);

                if (stepResult == WorkResult.Cancelled) return WorkResult.Cancelled;
            }

            return WorkResult.Completed;
        }
    }
}
