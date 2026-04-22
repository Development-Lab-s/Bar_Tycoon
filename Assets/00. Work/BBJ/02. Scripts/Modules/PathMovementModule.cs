using _00._Work._Resources._02._Scripts.Modules;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace BBJ.Modules
{
    public class PathMovementModule : MonoBehaviour, IModule, IPathMovement
    {
        public event Action MoveComplectedEvent;

        [SerializeField] private float moveSpeed = 1f;

        /// <summary>
        /// Vector3 == 부동소수점 비교 대신 사용하는 도달 판정 임계값.
        /// MoveTowards가 한 프레임에 이동하는 최소 거리보다 충분히 커야 한다.
        /// </summary>
        private const float ArrivalThreshold = 0.01f;

        private int      _targetIndex;
        private Vector3[] _path;
        private ModuleOwner _owner;

        public void Initialize(ModuleOwner owner) => _owner = owner;

        public void OnPathMove(Vector3[] newPath)
        {
            _path        = newPath;
            _targetIndex = 0;
            FollowPath().Forget();
        }

        private async UniTask FollowPath()
        {
            if (_path == null || _path.Length == 0)
            {
                MoveComplectedEvent?.Invoke();
                return;
            }

            while (_targetIndex < _path.Length)
            {
                Vector3 currentWaypoint = _path[_targetIndex];

                // Vector3 == 대신 거리 임계값으로 도달 판정
                if (Vector3.Distance(_owner.transform.position, currentWaypoint) <= ArrivalThreshold)
                {
                    _targetIndex++;
                    continue;
                }

                _owner.transform.position = Vector3.MoveTowards(
                    _owner.transform.position,
                    currentWaypoint,
                    moveSpeed * Time.fixedDeltaTime);

                await UniTask.WaitForFixedUpdate();
            }

            MoveComplectedEvent?.Invoke();
        }

#if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            if (_path == null) return;
            for (int i = _targetIndex; i < _path.Length; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(_path[i], Vector3.one);

                Gizmos.DrawLine(
                    i == _targetIndex ? _owner.transform.position : _path[i - 1],
                    _path[i]);
            }
        }
#endif
    }
}
