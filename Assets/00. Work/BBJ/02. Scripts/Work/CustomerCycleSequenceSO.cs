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
        [SerializeField] private WorkSO _takeSeatWork;
        [SerializeField] private WorkSO _waitOrderWork;
        [SerializeField] private WorkSO _waitForFoodWork;
        [SerializeField] private WorkSO _eatWork;
        [SerializeField] private WorkSO _payAtCounterWork;
        [SerializeField] private WorkSO _exitWork;

        public override async UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, CancellationToken ct)
        {
            if (_takeSeatWork     != null) await _takeSeatWork.ExecuteAsync(executor, context, ct);
            ct.ThrowIfCancellationRequested();
            if (_waitOrderWork    != null) await _waitOrderWork.ExecuteAsync(executor, context, ct);
            ct.ThrowIfCancellationRequested();
            if (_waitForFoodWork  != null) await _waitForFoodWork.ExecuteAsync(executor, context, ct);
            ct.ThrowIfCancellationRequested();
            if (_eatWork          != null) await _eatWork.ExecuteAsync(executor, context, ct);
            ct.ThrowIfCancellationRequested();
            if (_payAtCounterWork != null) await _payAtCounterWork.ExecuteAsync(executor, context, ct);
            ct.ThrowIfCancellationRequested();
            if (_exitWork         != null) await _exitWork.ExecuteAsync(executor, context, ct);
        }
    }
}
