using _00._Work._Resources._02._Scripts.Agents;
using BBJ.Order;
using BBJ.WorkplaceSystem;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace BBJ.Actions
{
    public class WorkAction : AgentActionBase
    {
        private WorkDurationUI _durationUI;

        public event Action OnWorkPhaseStarted;
        public event Action OnWorkPhaseEnded;
        public event Action<float> OnProgressUpdated;
        public bool IsInWorkPhase { get; private set; }

        public override void  InitOwner(Agent owner)
        {
            base.InitOwner(owner);
            _durationUI = owner.GetModule<WorkDurationUI>();
        }

        public async UniTask ExecuteAsync(Workplace workplace,OrderTicket orderTicket, CancellationToken ct)
        {
            var workExecutor = workplace.GetModule<IWorkExecutor>();
            if (workExecutor == null) return;

            void HandleProgress(float p)
            {
                _durationUI?.SetPercent(p);
                OnProgressUpdated?.Invoke(p);
            }

            _durationUI?.Active();
            workExecutor.OnProgressChanged += HandleProgress;

            IsInWorkPhase = true;
            OnWorkPhaseStarted?.Invoke();

            try
            {
                await workExecutor.ExecuteWorkAsync(_owner, orderTicket, ct);
            }
            finally
            {
                IsInWorkPhase = false;
                OnWorkPhaseEnded?.Invoke();

                workExecutor.OnProgressChanged -= HandleProgress;
                _durationUI?.Disable();
            }
        }
    }
}
