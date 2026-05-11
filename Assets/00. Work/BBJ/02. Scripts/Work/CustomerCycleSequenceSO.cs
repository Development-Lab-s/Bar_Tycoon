using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "CustomerCycleSequence", menuName = "Tycoon/Work/CustomerCycleSequence")]
    public class CustomerCycleSequenceSO : WorkSO
    {
        [SerializeField] private WorkSO[] _steps;

        public override async UniTask ExecuteAsync(
            ModuleOwner executor, GameEvent context, CancellationToken ct)
        {
            if (_steps == null) return;

            foreach (var step in _steps)
            {
                if (step != null)
                    await step.ExecuteAsync(executor, context, ct);
                ct.ThrowIfCancellationRequested();
            }
        }
    }
}
