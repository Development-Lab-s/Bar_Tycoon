using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace BBJ.Actions
{
    public class WaitAction : AgentActionBase
    {
        public WaitAction(Agent owner, AnimParamSO stateParam) : base(owner, stateParam) { }

        public override IEnumerator Execute(GameEvent param)
        {
            var e = param as WaitEvent;
            if (e.Condition != null)
                yield return new WaitUntil(e.Condition);
            else
                yield return new WaitForSeconds(e.Seconds);
        }

        public override async UniTask ExecuteAsync(GameEvent param, CancellationToken ct)
        {
            var e = param as WaitEvent;
            if (e == null) return;

            if (e.Condition != null)
                await UniTask.WaitUntil(e.Condition, cancellationToken: ct);
            else
                await UniTask.Delay(TimeSpan.FromSeconds(e.Seconds), cancellationToken: ct);
        }
    }
}
