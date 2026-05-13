using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "CustomerCycleSequence", menuName = "Tycoon/Work/CustomerCycleSequence")]
    public class CustomerCycleSequenceSO : WorkSO
    {
        [SerializeField] private WorkSO[] _steps;

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            if (_steps == null) return WorkResult.Completed;

            foreach (var step in _steps)
            {
                if (step == null) continue;

                WorkResult stepResult;
                try
                {
                    stepResult = await step.ExecuteAsync(executor, context, ctx);
                }
                catch (OperationCanceledException)
                {
                    stepResult = ctx.WasExternallyCompleted
                        ? WorkResult.ExternallyCompleted
                        : WorkResult.Cancelled;
                }

                step.OnResult(stepResult, executor, context);

                if (stepResult == WorkResult.Cancelled)           return WorkResult.Cancelled;
                if (stepResult == WorkResult.ExternallyCompleted) return WorkResult.Completed;
                // Completed -> next step
            }

            return WorkResult.Completed;
        }
    }
}
