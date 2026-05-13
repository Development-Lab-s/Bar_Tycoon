using BBJ.Actions;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "EatWork", menuName = "Tycoon/Work/Eat")]
    public class EatWorkSO : WorkSO
    {
        [SerializeField] private float _eatDuration = 8f;

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            try
            {
                if (agent != null)
                    await agent.WaitAsync(_eatDuration, ctx.Token);
                else
                    await UniTask.Delay(TimeSpan.FromSeconds(_eatDuration), cancellationToken: ctx.Token);
                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                return ctx.WasExternallyCompleted
                    ? WorkResult.ExternallyCompleted
                    : WorkResult.Cancelled;
            }
        }
    }
}
