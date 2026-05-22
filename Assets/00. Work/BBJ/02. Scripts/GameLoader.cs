using BBJ.Customer;
using BBJ.EventSystem;
using BBJ.Order;
using BBJ.Save;
using BBJ.Staff;
using Gamelib.EventSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Systems.SaveSystem;
using _00._Work.Lusaload._02._Scripts.SO;

namespace BBJ
{
    /// <summary>
    /// 세이브 유무를 확인하고 씬 복원 순서를 조율한다.
    /// 복원 순서: (ObjectManager 자체 Start) → Tickets → Customers → Link → Staff → _readyCount
    /// 종료 시 Orders+Staff를 game.save에 저장, StageLayout은 ObjectManager가 자체 저장.
    /// </summary>
    public class GameLoader : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] private OrderManager    _orderManager;
        [SerializeField] private CustomerManager _customerManager;
        [SerializeField] private StaffManager    _staffManager;

        [Header("Data")]
        [SerializeField] private CocktailRecipeDatabaseSO _database;

        [Header("PlayerHandle")]
        [SerializeField] private PlayerOrderHandle _playerOrderHandle;

        [Header("Scene")]
        [SerializeField] private EventChannelSO _sceneChannel;

        private const string SaveFile   = "game.save";
        private const string SaveFolder = "BarTycoon";

        private void Start()
        {
            if (SaveManager.IsSaveFile(SaveFile, SaveFolder))
                StartCoroutine(RestoreSequence());
            else
            {
                _staffManager?.SpawnAll();
                _sceneChannel?.RaiseEvent(new SceneReadyEvent());
            }
        }

        private void OnApplicationQuit()
        {
            // Staff 진행 중 작업 취소 후 저장 (CancellationToken 전파 → async 크래시 방지)
            _staffManager?.CancelAllWork();
            SaveAll();
        }

        // ─── 복원 ────────────────────────────────────────

        private IEnumerator RestoreSequence()
        {
            // ObjectManager.Start()가 Stage를 복원할 때까지 한 프레임 대기
            yield return null;

            var saveData = SaveManager.Load(typeof(GameSaveData), SaveFile, SaveFolder) as GameSaveData;
            if (saveData == null) yield break;

            // Step 2: OrderTicket 재생성 + OrderRegisterSO 등록
            var tickets = _orderManager != null
                ? _orderManager.RestoreTickets(saveData.Orders, _database)
                : new List<OrderTicket>();

            // Step 3: Customer 즉시 생성·텔레포트·점유
            var customers = _customerManager != null
                ? _customerManager.RestoreCustomers(saveData.Orders, _database)
                : new List<CustomerAgent>();

            // Step 4: Customer ↔ OrderTicket 연결 + Phase별 Dispatch
            _orderManager?.LinkTicketsToCustomers(tickets, customers);

            // Step 5: Staff 저장 위치에 스폰 (Timeline 없음)
            _staffManager?.RestoreStaff(saveData.Staff);

            // Step 6: _readyCount 재구성 (ReadyForServe 티켓 집계)
            _playerOrderHandle?.RebuildReadyCount(tickets);

            _sceneChannel?.RaiseEvent(new SceneReadyEvent());
        }

        // ─── 저장 ────────────────────────────────────────

        private void SaveAll()
        {
            var saveData = new GameSaveData
            {
                Orders = _orderManager?.GetOrdersSaveData() ?? new OrdersSaveData(),
                Staff  = _staffManager?.GetSaveData()       ?? new StaffSaveData(),
            };
            SaveManager.Save(saveData, SaveFile, SaveFolder);
        }
    }
}
