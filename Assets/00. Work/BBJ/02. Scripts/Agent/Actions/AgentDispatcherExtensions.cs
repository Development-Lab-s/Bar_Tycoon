using BBJ.Staff.FSM;
using BBJ.WorkplaceSystem;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace BBJ.Actions
{
    public static class AgentDispatcherExtensions
    {
        // --- Legacy IEnumerator ---
        public static IEnumerator MoveTo(this IActionDispatcher agent, Vector3 destination)
            => agent.ExecuteState(TycoonAgentAction.Move, new MoveEvent(destination));

        public static IEnumerator Wait(this IActionDispatcher agent, float seconds)
            => agent.ExecuteState(TycoonAgentAction.Wait, new WaitEvent(seconds));

        public static IEnumerator WaitUntil(this IActionDispatcher agent, Func<bool> condition)
            => agent.ExecuteState(TycoonAgentAction.Wait, new WaitEvent(condition));

        public static IEnumerator DoWork(this IActionDispatcher agent, Workplace workplace)
            => agent.ExecuteState(TycoonAgentAction.Work, new WorkEvent(workplace));

        // --- UniTask ---
        public static UniTask MoveAsync(this IActionDispatcher agent, Vector3 destination, CancellationToken ct)
            => agent.ExecuteStateAsync(TycoonAgentAction.Move, new MoveEvent(destination), ct);

        public static UniTask WaitAsync(this IActionDispatcher agent, float seconds, CancellationToken ct)
            => agent.ExecuteStateAsync(TycoonAgentAction.Wait, new WaitEvent(seconds), ct);

        public static UniTask WaitUntilAsync(this IActionDispatcher agent, Func<bool> condition, CancellationToken ct)
            => agent.ExecuteStateAsync(TycoonAgentAction.Wait, new WaitEvent(condition), ct);

        public static UniTask WaitUntilAsync(this IActionDispatcher agent, Func<bool> condition, CancellationToken ct, float timeout)
        {
            if (timeout <= 0f)
                return agent.ExecuteStateAsync(TycoonAgentAction.Wait, new WaitEvent(condition), ct);
            float end = UnityEngine.Time.time + timeout;
            return agent.ExecuteStateAsync(TycoonAgentAction.Wait, new WaitEvent(() => condition() || UnityEngine.Time.time >= end), ct);
        }

        public static UniTask DoWorkAsync(this IActionDispatcher agent, Workplace workplace, CancellationToken ct)
            => agent.ExecuteStateAsync(TycoonAgentAction.Work, new WorkEvent(workplace), ct);
    }
}
