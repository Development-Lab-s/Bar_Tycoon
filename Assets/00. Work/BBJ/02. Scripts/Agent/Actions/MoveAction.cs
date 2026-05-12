using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Movement;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Collections;
using System.Threading;

namespace BBJ.Actions
{
    public class MoveAction : AgentActionBase
    {
        private readonly IPathMovement _movement;
        private readonly IAgentPathProvider _pathProvider;

        public MoveAction(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _movement     = owner.GetModule<IPathMovement>();
            _pathProvider = owner as IAgentPathProvider;
        }

        public override IEnumerator Execute(GameEvent param)
        {
            var e   = param as MoveEvent;
            var tcs = new UniTaskCompletionSource();

            _movement.OnMoveCompleted += Complete;
            _pathProvider?.SetMoveDestination(e.Destination);

            yield return tcs.Task.ToCoroutine();
            _movement.OnMoveCompleted -= Complete;

            void Complete() => tcs.TrySetResult();
        }

        public override async UniTask ExecuteAsync(GameEvent param, CancellationToken ct)
        {
            var e   = param as MoveEvent;
            var tcs = new UniTaskCompletionSource();

            void Complete() => tcs.TrySetResult();
            _movement.OnMoveCompleted += Complete;
            _pathProvider?.SetMoveDestination(e.Destination);

            using (ct.Register(() => {
                _movement.StopMovement();
                tcs.TrySetResult(); 
            }))
                await tcs.Task;

            _movement.OnMoveCompleted -= Complete;
        }
    }
}
