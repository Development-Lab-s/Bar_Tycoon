using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Collections;
using UnityEngine;
using System.Threading;

namespace BBJ.Actions
{
    public class WorkAction : AgentActionBase
    {
        private Workplace _currentWorkplace;
        private WorkDurationUI durationUI;
        public WorkAction(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            durationUI = owner.GetModule<WorkDurationUI>();
        }

        public override IEnumerator Execute(GameEvent param)
        {
            _currentWorkplace = (param as WorkEvent)?.Workplace;
            if (_currentWorkplace == null) yield break;

            Enter();
            _currentWorkplace.GetModule<OccupancyModule>()?.Occupy(_owner);
            var workExecutor = _currentWorkplace.GetModule<IWorkExecutor>();
            if (workExecutor != null)
                yield return workExecutor.ExecuteWork(_owner);
            _currentWorkplace = null;
        }

        public override async UniTask ExecuteAsync(GameEvent param, CancellationToken ct)
        {
            _currentWorkplace = (param as WorkEvent)?.Workplace;
            if (_currentWorkplace == null) return;

            Enter();
            var workExecutor = _currentWorkplace.GetModule<IWorkExecutor>();
            Debug.Log( $"확인");
            Debug.Assert(workExecutor != null, $"{_currentWorkplace}에 workExecutor 누락");

            durationUI.Active();
            workExecutor.OnProgressChanged += durationUI.SetPercent;

            Debug.Log( $"일 시작");
            await workExecutor.ExecuteWorkAsync(_owner, ct);

            Debug.Log( $"일 종료");
            workExecutor.OnProgressChanged -= durationUI.SetPercent;
            durationUI.Disable();
            _currentWorkplace = null;
        }

        public override void Exit()
        {
            _currentWorkplace = null;
            base.Exit();
        }
    }
}
