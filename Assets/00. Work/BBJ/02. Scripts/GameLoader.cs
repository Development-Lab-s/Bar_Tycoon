using BBJ.Customer;
using BBJ.EventSystem;
using BBJ.GridSystem.Objects;
using BBJ.Order;
using BBJ.Save;
using BBJ.Staff;
using Gamelib.EventSystem;
using System.Collections.Generic;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Systems.SaveSystem;
using _00._Work.Lusaload._02._Scripts.SO;

namespace BBJ
{
    /// <summary>
    /// 저장 유무를 확인하고 씬 복원 순서를 조율한다.
    /// 복원 순서: Stage → Tickets → Customers → Link → Staff → ReadyCount
    /// 종료 시 Stage + Orders + Staff를 game.save 하나에 저장.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameLoader : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] private ObjectManager _objectManager;
        [SerializeField] private OrderManager _orderManager;
        [SerializeField] private CustomerManager _customerManager;
        [SerializeField] private StaffManager _staffManager;

        [Header("Data")]
        [SerializeField] private CocktailRecipeDatabaseSO _database;

        [Header("Player")]
        [SerializeField] private PlayerOrderHandle _playerOrderHandle;

        [Header("Scene")]
        [SerializeField] private EventChannelSO _sceneChannel;

        private const string SaveFile = "game.save";
        private const string SaveFolder = "BarTycoon";

        private void Start()
        {
            if (SaveManager.IsSaveFile(SaveFile, SaveFolder))
                RestoreFromSave();
            else
                StartFresh();
        }

        // ─── 새롭게 교체된 생명주기 저장 트리거 (OnDestroy 대체) ────────────────

        private void OnApplicationQuit()
        {
            Debug.Log("<color=orange>[GameLoader] OnApplicationQuit: 게임 종료를 감지했습니다.</color>");

            // 앱이 완전히 종료되므로 하던 일을 모두 취소(초기화)하고 저장
            _staffManager?.CancelAllWork();
            SaveAll();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Debug.Log("<color=orange>[GameLoader] OnApplicationPause: 백그라운드 전환을 감지했습니다.</color>");

                // 백그라운드로 전환될 때는 일을 취소하지 않고 '현재 상태'만 저장
                SaveAll();
            }
        }

        // ─── 신규 게임 ────────────────────────────────────────

        private void StartFresh()
        {
            _objectManager?.LoadDefaultLayout();
            _staffManager?.SpawnAll();
            _sceneChannel?.RaiseEvent(new SceneReadyEvent());
        }

        // ─── 복원 ────────────────────────────────────────────

        private void RestoreFromSave()
        {
            var saveData = SaveManager.Load(typeof(GameSaveData), SaveFile, SaveFolder) as GameSaveData;
            if (saveData == null)
            {
                StartFresh();
                return;
            }

            // Step 1: Stage 배치 복원
            _objectManager?.RestoreStage(saveData.Stage);

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

            // Step 4.5: 복원된 고객 사이클 시작
            _customerManager?.StartRestoredCycles(customers);

            // Step 5: Staff 저장 위치에 스폰
            _staffManager?.RestoreStaff(saveData.Staff);

            // Step 6: ReadyForServe 티켓 집계
            _playerOrderHandle?.RebuildReadyCount(tickets);

            _sceneChannel?.RaiseEvent(new SceneReadyEvent());
        }

        // ─── 저장 ─────────────────────────────────────────────

        private void SaveAll()
        {
            var saveData = new GameSaveData
            {
                Stage = _objectManager?.GetStageSaveData() ?? new StageSaveData(),
                Orders = _orderManager?.GetOrdersSaveData() ?? new OrdersSaveData(),
                Staff = _staffManager?.GetSaveData() ?? new StaffSaveData(),
            };
            SaveManager.Save(saveData, SaveFile, SaveFolder);
        }
    }
}