
using Gamelib.ObjectPool.Runtime;
using UnityEngine;

namespace BBJ.Tycoon.Board
{
    [CreateAssetMenu(fileName = "CustomerBoard", menuName = "Tycoon/SO/CustomerBoard")]
    public class CustomerBoardSO : ScriptableObject
    {
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
