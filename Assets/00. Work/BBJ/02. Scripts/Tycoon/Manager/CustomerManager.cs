using Gamelib.ObjectPool.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBJ.Tycoon
{
    /// <summary>
    /// 손님 스폰만 담당. 싱글톤 없음.
    /// 풀 반환은 CustomerAgent → CustomerBoardSO로 직접 처리.
    /// </summary>
    public class CustomerManager : MonoBehaviour
    {
        [Header("SO")]
        [SerializeField] private CustomerBoardSO _customerBoard;

        [Header("Pool")]
        [SerializeField] private PoolInitializer _poolInitializer;
        [SerializeField] private PoolItemSo      _customerPoolItem;

        [Header("Spawn Settings")]
        [SerializeField] private Transform        _spawnPoint;
        [SerializeField] private float            _spawnInterval = 8f;
        [SerializeField] private int              _maxCustomers  = 6;

        [Header("Menu")]
        [SerializeField] private List<FoodDataSO> _menuItems = new();

        private void Start() => StartCoroutine(SpawnLoop());

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(_spawnInterval);

                if (_customerBoard.ActiveCount >= _maxCustomers) continue;
                if (_menuItems.Count == 0) continue;

                SpawnCustomer();
            }
        }

        private void SpawnCustomer()
        {
            var customer = _poolInitializer.Pop<CustomerAgent>(_customerPoolItem);
            if (customer == null) return;

            customer.transform.position = _spawnPoint.position;
            _customerBoard.OnCustomerSpawned();

            var food = _menuItems[Random.Range(0, _menuItems.Count)];
            customer.StartCycle(food);
        }
    }
}
