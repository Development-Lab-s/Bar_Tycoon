using _00._Work._Resources._02._Scripts.Modules;
using BBJ.EventSystem;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System;
using System.Threading;
using UnityEngine;

namespace BBJ.Movement
{
    public class PathMovementModule : MonoBehaviour, IModule, IPathMovement
    {
        [SerializeField] private float moveSpeed = 1f;
        [SerializeField] private EventChannelSO PathRequestChannel;
        [SerializeField] [Range(0f, 1f)] private float minTurnSpeedRatio = 0.4f;

        private CancellationTokenSource _cts;
        private int _targetIndex;
        private Vector3[] _path;
        private Vector3 _prevDir;
        private ModuleOwner _owner;

        public bool IsMoving { get; private set; }
        public Vector3 Velocity { get; private set; }

        public event Action OnMoveStarted;
        public event Action<Vector3> OnMoveVelocityChanged;
        public event Action OnMoveCompleted;

        public void Initialize(ModuleOwner owner)
        {
            UtilDebugger.AssertAllAssigned(this);
            _owner = owner;
        }

        public void StartMove(Vector3[] path)
        {
            _cts.CancelAndDispose();
            _cts = new CancellationTokenSource();

            _targetIndex = 0;
            _path = path;
            _prevDir = Vector3.zero;
            FollowPath(_cts.Token).Forget();
        }

        public void StopMovement()
        {
            _cts.CancelAndDispose();
            _cts = null;
        }

        private async UniTask FollowPath(CancellationToken ct)
        {
            IsMoving = true;
            OnMoveStarted?.Invoke();
            try
            {
                if (_path == null || _path.Length == 0)
                {
                    FireCompleted();
                    return;
                }

                while (_targetIndex < _path.Length)
                {
                    Vector3 target = _path[_targetIndex];

                    Vector3 newDir = (target - _owner.transform.position).normalized;
                    float dot = _prevDir.sqrMagnitude > 0f ? Vector3.Dot(_prevDir, newDir) : 1f;
                    float speedMult = Mathf.Lerp(minTurnSpeedRatio, 1f, (dot + 1f) * 0.5f);
                    _prevDir = newDir;

                    OnMoveVelocityChanged?.Invoke(Velocity);
                    Velocity = target - _owner.transform.position;

                    float step = moveSpeed * speedMult * Time.fixedDeltaTime;
                    if (Vector3.Distance(_owner.transform.position, target) <= step)
                    {
                        _owner.transform.position = target;
                        _targetIndex++;
                        continue;
                    }

                    _owner.transform.position = Vector3.MoveTowards(_owner.transform.position, target, step);
                    await UniTask.WaitForFixedUpdate(cancellationToken: ct);
                }

                FireCompleted();
            }
            catch (OperationCanceledException) { }
            finally
            {
                IsMoving = false;
            }
        }

        public void OnPathMove(Vector3[] newPath) => StartMove(newPath);
        public void SetMoveDestination(Vector3 destination) => PathRequestChannel.RaiseEvent(new PathRequestEvent().Init(
                new(_owner.transform.position, destination, HandleRequestPath)));
        private void HandleRequestPath(Vector3[] path, bool isSucces)
        {
            if (!isSucces)
            {
                IsMoving = false;
                OnMoveCompleted?.Invoke();
                return;
            }
            OnPathMove(path);
        }
        private void FireCompleted() => OnMoveCompleted?.Invoke();
        public void OnSpeedChange(float speed) => this.moveSpeed = speed;

#if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            if (_path == null) return;
            for (int i = _targetIndex; i < _path.Length; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(_path[i], Vector3.one / 2f);
                Gizmos.DrawLine(
                    i == _targetIndex ? _owner.transform.position : _path[i - 1],
                    _path[i]);
            }
        }

#endif
    }
}
