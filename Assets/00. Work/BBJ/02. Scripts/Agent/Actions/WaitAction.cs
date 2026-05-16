using _00._Work._Resources._02._Scripts.Agents;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace BBJ.Actions
{
    public class WaitAction : AgentActionBase
    {
        public UniTask ExecuteAsync(float seconds, CancellationToken ct)
            => UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: ct);

        public UniTask ExecuteAsync(Func<bool> condition, CancellationToken ct)
            => UniTask.WaitUntil(condition, cancellationToken: ct);
    }
}
