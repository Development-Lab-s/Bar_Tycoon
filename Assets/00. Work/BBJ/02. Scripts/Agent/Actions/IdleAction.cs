using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Modules;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Schedule;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Collections;
using System.Threading;

namespace BBJ.Actions
{
    public class IdleAction : AgentActionBase
    {
        private EventChannelSO _scheduleTriggerChannel;

        public IdleAction(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _scheduleTriggerChannel = owner.GetModule<IScheduleTriggerSource>()?.ScheduleTriggerChannel;
        }

        public override IEnumerator Execute(GameEvent e)
        {
            _scheduleTriggerChannel?.RaiseEvent(new ScheduleTriggerEvent());
            yield break;
        }

        public override UniTask ExecuteAsync(GameEvent e, CancellationToken ct)
        {
            _scheduleTriggerChannel?.RaiseEvent(new ScheduleTriggerEvent());
            return UniTask.CompletedTask;
        }
    }
}
