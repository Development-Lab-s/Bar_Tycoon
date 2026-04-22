using Gamelib.ObjectPool.Runtime;
using UnityEngine;

namespace BBJ.Tycoon
{
    /// <summary>
    /// 손님 관련 런타임 상태 SO.
    ///
    /// 변경: _exitPoint에서 [SerializeField] 제거.
    ///   Initialize()에서 씬의 실제 위치로 덮어쓰므로 에디터 설정값은 무의미했음.
    ///   런타임 주입 전용임을 명시.
    /// </summary>
    [CreateAssetMenu(fileName = "CustomerBoard", menuName = "Tycoon/SO/CustomerBoard")]
    public class CustomerBoardSO : ScriptableObject
    {
        // 씬 초기화 시 주입되는 런타임 전용 값 — SerializeField 제거
        private Vector3 _exitPoint;
        public Vector3 ExitPoint => _exitPoint;

        private PoolInitializer _poolInitializer;

        private int _activeCount;
        public int ActiveCount => _activeCount;

        public void Initialize(PoolInitializer poolInitializer, Vector3 exitPoint)
        {
            _poolInitializer = poolInitializer;
            _exitPoint       = exitPoint;
            _activeCount     = 0;
        }

        public void OnCustomerSpawned() => _activeCount++;

        public void ReturnToPool(CustomerAgent customer)
        {
            _activeCount--;
            _poolInitializer?.Push(customer);
        }

        private void OnDisable()
        {
            _activeCount     = 0;
            _poolInitializer = null;
        }
    }
}
