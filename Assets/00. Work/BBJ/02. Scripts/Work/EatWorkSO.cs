using BBJ.Actions;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "EatWork", menuName = "Tycoon/Work/Eat")]
    public class EatWorkSO : WorkSO
    {
        [SerializeField] private float _eatDuration = 8f;

        public override UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, CancellationToken ct)
        {
            var agent = executor as IActionDispatcher;
            if (agent != null)
                return agent.WaitAsync(_eatDuration, ct);
            return UniTask.Delay(TimeSpan.FromSeconds(_eatDuration), cancellationToken: ct);
        }
    }
}
