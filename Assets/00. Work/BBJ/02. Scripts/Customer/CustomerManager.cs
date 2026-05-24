using BBJ.EventSystem;
using BBJ.Order;
using BBJ.Register;
using BBJ.Save;
using BBJ.Schedule;
using BBJ.Work;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Gamelib.EventSystem;
using Gamelib.ObjectPool.Runtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _00._Work.Lusaload._02._Scripts.SO;

namespace BBJ.Customer
{
    public class CustomerManager : MonoBehaviour
    {
        [Header("SO")]
        [SerializeField] private EventChannelSO _customerChannel;

        [Header("Pool")]
        [SerializeField] private PoolManagerSo _poolManager;
        [SerializeField] private PoolItemSo _customerPoolItem;

        [Header("Spawn Settings")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private float _spawnInterval = 8f;
        [SerializeField] private int _maxCustomers = 6;

        [Header("Cycle")]
        [SerializeField] private WorkSO _cycleSequence;

        [Header("Menu")]
        [SerializeField] private CocktailRecipeDatabaseSO _database;
        [SerializeField] private int _currentStage = 1;

        [Header("Restore")]
        [SerializeField] private WorkplaceRegisterSO _workplaceRegister;
        [SerializeField] private WorkplaceTypeSO _seatType;

        private int _activeCount;

        private void Awake()
        {
            UtilDebugger.AssertAllAssigned(this);
            _activeCount = 0;
            _customerChannel.AddListener<CustomerLeftEvent>(HandleCustomerLeft);
        }

        private void OnDestroy()
        {
            _customerChannel.RemoveListener<CustomerLeftEvent>(HandleCustomerLeft);
        }

        private void Start()
        {
            StartCoroutine(SpawnLoop());
        }

        public void SetStage(int stage) => _currentStage = Mathf.Max(1, stage);

        private void HandleCustomerLeft(CustomerLeftEvent evt)
        {
            _activeCount--;
            _poolManager.Push(evt.Customer);
        }

        private IEnumerator SpawnLoop()
        {
            Debug.Log("싸이클 시작");
            while (true)
            {
                yield return new WaitForSeconds(_spawnInterval);

                if (_activeCount >= _maxCustomers) continue;
                if (_database == null || _cycleSequence == null) continue;
                Debug.Log("손님 소환");

                try { SpawnCustomer(); }
                catch (System.Exception e) { Debug.LogWarning("[CustomerManager] SpawnCustomer failed: " + e.Message); }
            }
        }

        private void SpawnCustomer()
        {
            CustomerAgent customer = _poolManager.Pop<CustomerAgent>(_customerPoolItem);
            if (customer == null) return;

            CocktailRecipeSO recipe = PickWeightedRandom(_currentStage);
            if (recipe == null) return;

            customer.transform.position = _spawnPoint.position;
            _activeCount++;

            customer.StartCycle(recipe);
            customer.GetModule<SchedulingModule>()?.AssignWork(_cycleSequence, null);
        }

        // ─── 복원 API ────────────────────────────────────

        public List<CustomerAgent> RestoreCustomers(OrdersSaveData data, CocktailRecipeDatabaseSO database)
        {
            var customers = new List<CustomerAgent>(data.Tickets.Count);

            var availableSeats = _workplaceRegister != null && _seatType != null
                ? _workplaceRegister.GetAll(_seatType)
                : new List<Workplace>();

            int seatIndex = 0;
            foreach (var save in data.Tickets)
            {
                var recipe = database?.recipes.FirstOrDefault(r => r.name == save.RecipeId);
                if (recipe == null) continue;

                var customer = _poolManager.Pop<CustomerAgent>(_customerPoolItem);
                if (customer == null) continue;

                if (save.WorkPhase != OrderWorkPhase.ReadyForCashier)
                {
                    if (seatIndex >= availableSeats.Count) { _poolManager.Push(customer); continue; }

                    var seat      = availableSeats[seatIndex++];
                    var seatModule = seat.GetModule<SeatModule>();
                    var occupancy  = seat.GetModule<OccupancyModule>();
                    if (seatModule == null || occupancy == null) { _poolManager.Push(customer); continue; }

                    occupancy.TryReserve(customer, null);
                    occupancy.Occupy(customer);
                    seatModule.AssignCustomer(customer);

                    var role = customer.GetModule<SchedulingModule>()?.InteractRole;
                    customer.transform.position = seat.GetNearestPoint(role, _spawnPoint.position);
                    seatModule.Seat(customer);
                    customer.AssignedSeat = seat;

                    if (save.WorkPhase == OrderWorkPhase.Eating)
                        customer.SetFoodServedForRestore();
                }
                else
                {
                    customer.transform.position = _spawnPoint.position;
                    customer.SetFoodServedForRestore();
                }

                customer.RestoreCycleStep = save.WorkPhase switch
                {
                    OrderWorkPhase.ReadyForServer  => 1,
                    OrderWorkPhase.ReadyForServe   => 2,
                    OrderWorkPhase.Eating          => 3,
                    OrderWorkPhase.ReadyForCashier => 4,
                    _                              => 1,
                };

                _activeCount++;
                customers.Add(customer);
            }

            return customers;
        }

        public void StartRestoredCycles(List<CustomerAgent> customers)
        {
            foreach (var customer in customers)
            {
                customer.ChangeState(CustomerState.Idle);
                customer.GetModule<SchedulingModule>()?.AssignWork(_cycleSequence, null);
            }
        }

        // r ∈ [1, 2^n - 1] 균일 샘플 → 비트 길이(r) = 스테이지
        // stage k 의 슬롯 수 = 2^(k-1)  →  높은 스테이지일수록 높은 확률
        private CocktailRecipeSO PickWeightedRandom(int currentStage)
        {
            int n = Mathf.Clamp(currentStage, 1, 30);
            int r = Random.Range(1, 1 << n);

            int stage = 0, temp = r;
            while (temp > 0) { temp >>= 1; stage++; }

            // unlockStage 0은 stage 1과 같은 버킷으로 취급
            var candidates = _database.Recipes
                .Where(c => c.unlockStage == stage || (stage == 1 && c.unlockStage == 0))
                .ToList();

            if (candidates.Count == 0)
                candidates = _database.Recipes.Where(c => c.unlockStage <= currentStage).ToList();

            if (candidates.Count == 0) return null;
            return candidates[Random.Range(0, candidates.Count)];
        }
    }
}
