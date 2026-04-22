using BBJ.GridSystem.Pathfind;
using UnityEngine;

namespace BBJ.Tycoon.Board
{
    [CreateAssetMenu(fileName = "PathRequestSO", menuName = "Tycoon/SO/PathRequest")]
    public class PathRequestSO : ScriptableObject
    {
        private PathRequestManager _manager;

        public void Initialize(PathRequestManager manager) => _manager = manager;

        public void RequestPath(Vector3 start, Vector3 end, PathRequestManager.PathRequestAction callback)
        {
            if (_manager == null)
            {
                Debug.LogError("[PathRequestSO] PathRequestManager가 주입되지 않았습니다.");
                callback?.Invoke(new Vector3[0], false);
                return;
            }
            _manager.RequestPath(start, end, callback);
        }

        private void OnDisable() => _manager = null;
    }
}
