using BBJ.GridSystem.Pathfind;
using Gamelib.ObjectPool.Runtime;
using UnityEngine;

namespace BBJ.Tycoon
{
    public class SceneInitializer : MonoBehaviour
    {
        [Header("SO References")]
        [SerializeField] private PathRequestSO   _pathRequestSO;
        [SerializeField] private CounterBoardSO  _counterBoardSO;
        [SerializeField] private CustomerBoardSO _customerBoardSO;

        [Header("Scene Objects")]
        [SerializeField] private PathRequestManager _pathRequestManager;
        [SerializeField] private CounterWorkplace   _counter;
        [SerializeField] private PoolInitializer    _poolInitializer;
        [SerializeField] private Transform          _exitPoint;

        private void Awake()
        {
            _pathRequestSO.Initialize(_pathRequestManager);
            _counterBoardSO.Initialize(_counter);
            _customerBoardSO.Initialize(_poolInitializer, _exitPoint.position);
        }
    }
}
