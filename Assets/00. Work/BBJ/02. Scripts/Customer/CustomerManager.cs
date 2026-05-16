using BBJ.Data;
using BBJ.Schedule;
using BBJ.Work;
using Gamelib.EventSystem;
using Gamelib.ObjectPool.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBJ.Customer
{
    public class CustomerManager : MonoBehaviour
    {
        [Header("SO")]
        [SerializeField] private EventChannelSO _customerChannel;

        [Header("Pool")]
        [SerializeField] private PoolInitializer _poolInitializer;
        [SerializeField] private PoolItemSo _customerPoolItem;

        [Header("Spawn Settings")]
        [SerializeField] private Transform _spawnPoint;
        // 묶어서 SO로 관리할 예정
        [SerializeField] private float _spawnInterval = 8f;
        [SerializeField] private int _maxCustomers = 6;

        [Header("Cycle")]
        [SerializeField] private WorkSO _cycleSequence;

        [Header("Menu")]
        [SerializeField] private List<FoodDataSO> _menuItems = new();

        private int _activeCount;
        private void Awake()
        {
            UtilDebugger.AssertAllAssigned(this);

            _activeCount = 0;
            _customerChannel.AddListener<CustomerLeftEvent>(HandleCustomerLeft);
            _poolInitializer.PoolManager.InitializePool(_poolInitializer.transform);
        }
        private void OnDestroy()
        {
            _customerChannel.RemoveListener<CustomerLeftEvent>(HandleCustomerLeft);
        }

        private void Start()
        {
            // SO의 런타임 딕셔너리가 플레이모드 진입 중 리셋될 수 있어 Start에서 재보장
            StartCoroutine(SpawnLoop());
        }

        private void HandleCustomerLeft(CustomerLeftEvent evt)
        {
            _activeCount--;
            _poolInitializer.Push(evt.Customer);
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(_spawnInterval);

                if (_activeCount >= _maxCustomers) continue;
                if (_menuItems.Count == 0 || _cycleSequence == null) continue;

                try { SpawnCustomer(); }
                catch (System.Exception e) { Debug.LogWarning("[CustomerManager] SpawnCustomer failed: " + e.Message); }
            }
        }

        private void SpawnCustomer()
        {
            CustomerAgent customer = _poolInitializer.Pop<CustomerAgent>(_customerPoolItem);
            if (customer == null) return;

            customer.transform.position = _spawnPoint.position;
            _activeCount++;

            FoodDataSO food = _menuItems[Random.Range(0, _menuItems.Count)];
            customer.StartCycle(food);
            customer.GetModule<SchedulingModule>()?.AssignWork(_cycleSequence, null);
        }
    }
}
