using _00._Work._Resources._02._Scripts.Modules;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace BBJ.Movement
{
    public class PathMovementModule : MonoBehaviour, IModule, IPathMovement
    {
        public event Action OnMoveCompleted;

        [SerializeField] private float moveSpeed = 1f;

        private const float ArrivalThreshold = 0.01f;

        private int         _generation;
        private int         _targetIndex;
        private Vector3[]   _path;

        private ModuleOwner _owner;

        public bool IsMoving { get; private set; }

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
        }

        public void OnSpeedChanged(float speed) => this.moveSpeed = speed;
        public void OnPathMove(Vector3[] newPath) => StartMove(newPath);
        public void StartMove(Vector3[] path)
        {
            _path        = path;
            _targetIndex = 0;
            _generation++;
            FollowPath(_generation).Forget();
        }

        public void StopMovement()
        {
            _generation++;
            IsMoving = false;
        }

        private async UniTask FollowPath(int gen)
        {
            IsMoving = true;

            if (_path == null || _path.Length == 0)
            {
                IsMoving = false;
                if (_generation == gen) FireCompleted();
                return;
            }

            while (_targetIndex < _path.Length)
            {
                if (_generation != gen) { IsMoving = false; return; }

                Vector3 target = _path[_targetIndex];

                if (Vector3.Distance(_owner.transform.position, target) <= ArrivalThreshold)
                {
                    _targetIndex++;
                    continue;
                }

                _owner.transform.position = Vector3.MoveTowards(
                    _owner.transform.position,
                    target,
                    moveSpeed * Time.fixedDeltaTime);

                await UniTask.WaitForFixedUpdate();
            }

            if (_generation != gen) { IsMoving = false; return; }

            IsMoving = false;
            FireCompleted();
        }

        private void FireCompleted()
        {
            OnMoveCompleted?.Invoke();
        }

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
