using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.WorkplaceSystem;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Collections;
using System.Threading;

namespace BBJ.Actions
{
    public class WorkAction : AgentActionBase
    {
        private Workplace _currentWorkplace;

        public WorkAction(Agent owner, AnimParamSO stateParam) : base(owner, stateParam) { }

        public override IEnumerator Execute(GameEvent param)
        {
            _currentWorkplace = (param as WorkEvent)?.Workplace;
            if (_currentWorkplace == null) yield break;

            Enter();
            _currentWorkplace.Occupy(_owner);
            yield return _currentWorkplace.ExecuteWork(_owner);
            _currentWorkplace = null;
        }

        public override async UniTask ExecuteAsync(GameEvent param, CancellationToken ct)
        {
            _currentWorkplace = (param as WorkEvent)?.Workplace;
            if (_currentWorkplace == null) return;

            Enter();
            await _currentWorkplace.ExecuteWorkAsync(_owner, ct);
            _currentWorkplace = null;
        }

        public override void Exit()
        {
            _currentWorkplace = null;
            base.Exit();
        }
    }
}
