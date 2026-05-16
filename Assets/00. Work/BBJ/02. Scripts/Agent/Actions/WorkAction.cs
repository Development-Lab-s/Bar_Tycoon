using _00._Work._Resources._02._Scripts.Agents;
using BBJ.WorkplaceSystem;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace BBJ.Actions
{
    public class WorkAction : AgentActionBase
    {
        private WorkDurationUI _durationUI;

        public override void  InitOwner(Agent owner)
        {
            base.InitOwner(owner);
            _durationUI = owner.GetModule<WorkDurationUI>();
        }

        public async UniTask ExecuteAsync(Workplace workplace, CancellationToken ct)
        {
            var workExecutor = workplace.GetModule<IWorkExecutor>();
            if (workExecutor == null) return;

            _durationUI?.Active();
            if (_durationUI != null)
                workExecutor.OnProgressChanged += _durationUI.SetPercent;

            await workExecutor.ExecuteWorkAsync(_owner, ct);

            if (_durationUI != null)
                workExecutor.OnProgressChanged -= _durationUI.SetPercent;
            _durationUI?.Disable();
        }
    }
}
