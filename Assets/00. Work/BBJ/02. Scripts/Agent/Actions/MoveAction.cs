using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.GridSystem.Pathfind;
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
        private readonly RuntimeReference<IPathRequestManager> _pathRequest;

        public MoveAction(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _movement    = owner.GetModule<IPathMovement>();
            _pathRequest = (owner as IAgentPathProvider)?.PathRequest;
        }

        public override IEnumerator Execute(GameEvent param)
        {
            var e   = param as MoveEvent;
            var tcs = new UniTaskCompletionSource();

            _movement.OnMoveCompleted += Complete;
            _pathRequest.Instance.RequestPath(_owner.transform.position, e.Destination,
                (path, success) =>
                {
                    if (success && path.Length > 0) _movement.OnPathMove(path);
                    else Complete();
                });

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

            _pathRequest.Instance.RequestPath(_owner.transform.position, e.Destination,
                (path, success) =>
                {
                    if (ct.IsCancellationRequested) { tcs.TrySetResult(); return; }
                    if (success && path.Length > 0) _movement.OnPathMove(path);
                    else tcs.TrySetResult();
                });

            using (ct.Register(() => { _movement.StopMovement(); tcs.TrySetResult(); }))
                await tcs.Task;

            _movement.OnMoveCompleted -= Complete;
        }
    }
}
