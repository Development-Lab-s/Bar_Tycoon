using _00._Work._Resources._02._Scripts.Agents;
using BBJ.Movement;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace BBJ.Actions
{
    public class MoveAction : AgentActionBase
    {
        private IPathMovement _movement;

        public override void InitOwner(Agent owner) 
        {
            base.InitOwner(owner);
            _movement = owner.GetModule<IPathMovement>();
        }

        public async UniTask ExecuteAsync(Vector3 destination, CancellationToken ct)
        {
            var tcs = new UniTaskCompletionSource();

            void Complete() => tcs.TrySetResult();
            _movement.OnMoveCompleted += Complete;
            _movement.SetMoveDestination(destination);

            using (ct.Register(() =>
            {
                _movement.StopMovement();
                tcs.TrySetResult();
            }))
                await tcs.Task;

            _movement.OnMoveCompleted -= Complete;
        }
    }
}
