using BBJ.Actions;
using BBJ.Customer;
using BBJ.EventSystem;
using BBJ.Modules;
using BBJ.Order;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "EatWork", menuName = "Tycoon/Work/Eat")]
    public class EatWorkSO : WorkSO
    {
        [SerializeField] private float _eatDuration = 8f;

        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var actions  = executor.GetModule<AgentActionModule>();
            if (actions != null)
                await actions.Execute<WaitAction>(a => a.ExecuteAsync(_eatDuration, ctx.Token));
            else
                await UniTask.Delay(TimeSpan.FromSeconds(_eatDuration), cancellationToken: ctx.Token);
            
            var activeTicket = customer?.ActiveTicket;
            if (activeTicket != null && !activeTicket.IsTerminal && activeTicket.WorkPhase == OrderWorkPhase.Eating)
                _ctx.OrderChannel?.RaiseEvent(new CustomerEatCompleteEvent(activeTicket));

            return WorkResult.Completed;
        }
    }
}
