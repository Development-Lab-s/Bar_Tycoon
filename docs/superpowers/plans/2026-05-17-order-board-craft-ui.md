# Order Board Craft UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `PendingCook` 단계 주문을 음식 종류별로 묶어 표시하는 조리 스테이션 UI를 구현하고, 플레이어가 점유·뺏기를 통해 미니게임을 시작할 수 있게 한다.

**Architecture:** `PlayerOrderHandle`(MonoBehaviour)이 이벤트 채널을 구독해 두 딕셔너리를 유지하고, `OrderBoardCraftUI`가 이를 읽어 `FoodGroupCardUI` 카드를 생성한다. 점유 뺏기 시 `StealConfirmUI` 팝업으로 확인을 받는다.

**Tech Stack:** Unity 2D, UGUI (TMP, Button, Image), EventChannelSO, C# Dictionary

---

## 파일 구조

| 경로 | 상태 | 역할 |
|---|---|---|
| `Assets/00. Work/BBJ/02. Scripts/Event/OrderEvents.cs` | 수정 | `PlayerCraftStartEvent` 추가 |
| `Assets/00. Work/BBJ/02. Scripts/Order/PlayerOrderHandle.cs` | 신규 | 플레이어 점유 핸들, 두 딕셔너리 관리 |
| `Assets/00. Work/BBJ/02. Scripts/UI/Order/FoodGroupCardUI.cs` | 신규 | 음식 종류별 카드 (미점유 / 점유 외형 전환) |
| `Assets/00. Work/BBJ/02. Scripts/UI/Order/StealConfirmUI.cs` | 신규 | 점유 뺏기 확인 팝업 |
| `Assets/00. Work/BBJ/02. Scripts/UI/Order/OrderBoardCraftUI.cs` | 신규 | 조리 주문 보드 패널 |

---

## Task 1: PlayerCraftStartEvent 추가

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Event/OrderEvents.cs`

- [ ] **Step 1: `OrderEvents.cs` 파일 끝에 이벤트 클래스 추가**

파일 마지막 `}` 직전에 아래를 추가한다.

```csharp
    public class PlayerCraftStartEvent : GameEvent
    {
        public OrderTicket Ticket { get; }
        public PlayerCraftStartEvent(OrderTicket ticket) => Ticket = ticket;
    }
```

최종 파일에서 `namespace BBJ.EventSystem { ... }` 안에 다른 이벤트들과 나란히 위치한다.

- [ ] **Step 2: Unity 콘솔에서 컴파일 오류 없음 확인**

Unity Editor를 열고 Console 창에 빨간 오류가 없으면 통과.

- [ ] **Step 3: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Event/OrderEvents.cs"
git commit -m "feat: add PlayerCraftStartEvent"
```

---

## Task 2: PlayerOrderHandle 생성

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/Order/PlayerOrderHandle.cs`

- [ ] **Step 1: 파일 생성**

```csharp
using BBJ.Data;
using BBJ.EventSystem;
using BBJ.Order;
using Gamelib.EventSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Order
{
    public class PlayerOrderHandle : MonoBehaviour
    {
        [SerializeField] private ModuleOwner    _playerOwner;
        [SerializeField] private EventChannelSO _orderChannel;

        private readonly Dictionary<FoodDataSO, int>               _readyCount     = new();
        private readonly Dictionary<FoodDataSO, List<OrderTicket>> _pendingTickets = new();

        private void OnEnable()
        {
            _orderChannel?.AddListener<OrderRegisteredEvent>(OnOrderRegistered);
            _orderChannel?.AddListener<OrderUnregisteredEvent>(OnOrderUnregistered);
            _orderChannel?.AddListener<OrderStateChangedEvent>(OnOrderStateChanged);
        }

        private void OnDisable()
        {
            _orderChannel?.RemoveListener<OrderRegisteredEvent>(OnOrderRegistered);
            _orderChannel?.RemoveListener<OrderUnregisteredEvent>(OnOrderUnregistered);
            _orderChannel?.RemoveListener<OrderStateChangedEvent>(OnOrderStateChanged);
        }

        // --- 쿼리 ---

        public OrderTicket GetFreeTicket(FoodDataSO food)
        {
            if (!_pendingTickets.TryGetValue(food, out var list)) return null;
            return list.FirstOrDefault(t => t.State == OrderState.Waiting);
        }

        public OrderTicket GetOccupiedTicket(FoodDataSO food)
        {
            if (!_pendingTickets.TryGetValue(food, out var list)) return null;
            return list.FirstOrDefault(t => t.State is OrderState.Reserved or OrderState.InProgress);
        }

        public bool HasFreeTicket(FoodDataSO food)     => GetFreeTicket(food) != null;
        public bool HasOccupiedTicket(FoodDataSO food) => GetOccupiedTicket(food) != null;

        public IReadOnlyList<OrderTicket> GetPendingTickets(FoodDataSO food)
        {
            if (!_pendingTickets.TryGetValue(food, out var list))
                return System.Array.Empty<OrderTicket>();
            return list;
        }

        public IEnumerable<FoodDataSO> GetAllPendingFoods() => _pendingTickets.Keys;

        // --- 액션 ---

        public bool TryOccupy(OrderTicket ticket) => ticket.TryReserve(_playerOwner);

        public bool TrySteal(OrderTicket ticket)
        {
            if (!ticket.TrySteal(_playerOwner)) return false;
            _orderChannel?.RaiseEvent(new OrderStateChangedEvent(ticket));
            return true;
        }

        public void AddReadyFood(FoodDataSO food)
        {
            if (food == null) return;
            _readyCount.TryGetValue(food, out int count);
            _readyCount[food] = count + 1;
        }

        public void ConsumeReadyFood(FoodDataSO food)
        {
            if (food == null || !_readyCount.TryGetValue(food, out int count) || count <= 0) return;
            _readyCount[food] = count - 1;
        }

        public int GetReadyCount(FoodDataSO food)
        {
            _readyCount.TryGetValue(food, out int count);
            return count;
        }

        // --- 이벤트 핸들러 ---

        private void OnOrderRegistered(OrderRegisteredEvent e)
        {
            if (e.Ticket.WorkPhase != OrderWorkPhase.PendingCook) return;
            AddToPending(e.Ticket);
        }

        private void OnOrderUnregistered(OrderUnregisteredEvent e)
        {
            RemoveFromPending(e.Ticket);
        }

        private void OnOrderStateChanged(OrderStateChangedEvent e)
        {
            if (e.Ticket.WorkPhase != OrderWorkPhase.PendingCook)
                RemoveFromPending(e.Ticket);
            else
                AddToPending(e.Ticket);
        }

        private void AddToPending(OrderTicket ticket)
        {
            var food = ticket.Food;
            if (!_pendingTickets.TryGetValue(food, out var list))
            {
                list = new List<OrderTicket>();
                _pendingTickets[food] = list;
            }
            if (!list.Contains(ticket))
                list.Add(ticket);
        }

        private void RemoveFromPending(OrderTicket ticket)
        {
            var food = ticket.Food;
            if (!_pendingTickets.TryGetValue(food, out var list)) return;
            list.Remove(ticket);
            if (list.Count == 0)
                _pendingTickets.Remove(food);
        }
    }
}
```

- [ ] **Step 2: Unity 콘솔에서 컴파일 오류 없음 확인**

- [ ] **Step 3: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Order/PlayerOrderHandle.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Order/PlayerOrderHandle.cs.meta"
git commit -m "feat: add PlayerOrderHandle for player order occupation"
```

---

## Task 3: FoodGroupCardUI 생성

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/UI/Order/FoodGroupCardUI.cs`

- [ ] **Step 1: 파일 생성**

```csharp
using BBJ.Data;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBJ.UI.Order
{
    public class FoodGroupCardUI : MonoBehaviour
    {
        [SerializeField] private Image    _foodIcon;
        [SerializeField] private TMP_Text _foodName;
        [SerializeField] private TMP_Text _countLabel;
        [SerializeField] private Image    _occupiedBadge;
        [SerializeField] private Button   _button;

        private FoodDataSO         _food;
        private Action<FoodDataSO> _onClick;

        private void Awake()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        public void Setup(FoodDataSO food, int count, bool isOccupied, Action<FoodDataSO> onClick)
        {
            _food    = food;
            _onClick = onClick;

            _foodIcon.sprite = food.Icon;
            _foodName.text   = food.FoodName;

            Refresh(count, isOccupied);
        }

        public void Refresh(int count, bool isOccupied)
        {
            _occupiedBadge.gameObject.SetActive(isOccupied);
            _countLabel.gameObject.SetActive(!isOccupied);

            if (!isOccupied)
                _countLabel.text = $"x{count}";
        }

        private void OnClicked() => _onClick?.Invoke(_food);
    }
}
```

- [ ] **Step 2: Unity에서 컴파일 오류 없음 확인**

- [ ] **Step 3: Prefab 제작**

  1. Hierarchy에 UI > Panel 생성 → 이름 `FoodGroupCard`
  2. 자식으로 추가:
     - `Image` → `FoodIcon` (음식 아이콘)
     - `TMP_Text` → `FoodName`
     - `TMP_Text` → `CountLabel` ("x2" 표시용)
     - `Image` → `OccupiedBadge` ("점유" 표시, 기본 비활성)
     - `Button` (Panel 자체 또는 별도 버튼)
  3. `FoodGroupCardUI` 컴포넌트를 루트에 부착
  4. Inspector에서 각 필드를 연결
  5. Prefab으로 저장: `Assets/00. Work/BBJ/Prefabs/UI/FoodGroupCard.prefab`

- [ ] **Step 4: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/UI/Order/FoodGroupCardUI.cs"
git add "Assets/00. Work/BBJ/02. Scripts/UI/Order/FoodGroupCardUI.cs.meta"
git commit -m "feat: add FoodGroupCardUI"
```

---

## Task 4: StealConfirmUI 생성

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/UI/Order/StealConfirmUI.cs`

- [ ] **Step 1: 파일 생성**

```csharp
using BBJ.Order;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBJ.UI.Order
{
    public class StealConfirmUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _messageLabel;
        [SerializeField] private Button   _confirmButton;
        [SerializeField] private Button   _cancelButton;

        private Action _onConfirm;
        private Action _onCancel;

        private void Awake()
        {
            _confirmButton.onClick.AddListener(OnConfirm);
            _cancelButton.onClick.AddListener(OnCancel);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _confirmButton.onClick.RemoveListener(OnConfirm);
            _cancelButton.onClick.RemoveListener(OnCancel);
        }

        public void Show(OrderTicket ticket, Action onConfirm, Action onCancel)
        {
            _messageLabel.text = $"[{ticket.Food?.FoodName}] 이미 점유 중입니다.\n작업을 빼앗겠습니까?";
            _onConfirm = onConfirm;
            _onCancel  = onCancel;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            _onConfirm = null;
            _onCancel  = null;
        }

        private void OnConfirm()
        {
            var cb = _onConfirm;
            Hide();
            cb?.Invoke();
        }

        private void OnCancel()
        {
            var cb = _onCancel;
            Hide();
            cb?.Invoke();
        }
    }
}
```

- [ ] **Step 2: Unity에서 컴파일 오류 없음 확인**

- [ ] **Step 3: Prefab 제작**

  1. Hierarchy에 UI > Panel 생성 → 이름 `StealConfirmPanel`
  2. 자식으로 추가:
     - `TMP_Text` → `MessageLabel`
     - `Button` → `ConfirmButton` (텍스트: "빼앗기")
     - `Button` → `CancelButton` (텍스트: "취소")
  3. `StealConfirmUI` 컴포넌트를 루트에 부착, 필드 연결
  4. 저장: `Assets/00. Work/BBJ/Prefabs/UI/StealConfirmPanel.prefab`
  5. Prefab이 기본적으로 비활성 상태인지 확인 (Awake에서 SetActive(false) 처리됨)

- [ ] **Step 4: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/UI/Order/StealConfirmUI.cs"
git add "Assets/00. Work/BBJ/02. Scripts/UI/Order/StealConfirmUI.cs.meta"
git commit -m "feat: add StealConfirmUI"
```

---

## Task 5: OrderBoardCraftUI 생성

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/UI/Order/OrderBoardCraftUI.cs`

- [ ] **Step 1: 파일 생성**

```csharp
using BBJ.Data;
using BBJ.EventSystem;
using BBJ.Order;
using Gamelib.EventSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBJ.UI.Order
{
    public class OrderBoardCraftUI : MonoBehaviour
    {
        [SerializeField] private PlayerOrderHandle _handle;
        [SerializeField] private FoodGroupCardUI   _cardPrefab;
        [SerializeField] private StealConfirmUI    _confirmUI;
        [SerializeField] private EventChannelSO    _orderChannel;
        [SerializeField] private Transform         _content;
        [SerializeField] private Button            _cancelButton;

        private readonly Dictionary<FoodDataSO, FoodGroupCardUI> _freeCards     = new();
        private readonly Dictionary<FoodDataSO, FoodGroupCardUI> _occupiedCards = new();

        private void Awake()
        {
            _cancelButton.onClick.AddListener(Close);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _cancelButton.onClick.RemoveListener(Close);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            SubscribeEvents();
            RebuildAllCards();
        }

        public void Close()
        {
            UnsubscribeEvents();
            ClearAllCards();
            gameObject.SetActive(false);
        }

        // --- 이벤트 구독 ---

        private void SubscribeEvents()
        {
            _orderChannel?.AddListener<OrderRegisteredEvent>(OnOrderRegistered);
            _orderChannel?.AddListener<OrderUnregisteredEvent>(OnOrderUnregistered);
            _orderChannel?.AddListener<OrderStateChangedEvent>(OnOrderStateChanged);
        }

        private void UnsubscribeEvents()
        {
            _orderChannel?.RemoveListener<OrderRegisteredEvent>(OnOrderRegistered);
            _orderChannel?.RemoveListener<OrderUnregisteredEvent>(OnOrderUnregistered);
            _orderChannel?.RemoveListener<OrderStateChangedEvent>(OnOrderStateChanged);
        }

        // --- 카드 빌드 ---

        private void RebuildAllCards()
        {
            ClearAllCards();
            foreach (var food in _handle.GetAllPendingFoods())
                RefreshCardsForFood(food);
        }

        private void ClearAllCards()
        {
            foreach (var card in _freeCards.Values)   Destroy(card.gameObject);
            foreach (var card in _occupiedCards.Values) Destroy(card.gameObject);
            _freeCards.Clear();
            _occupiedCards.Clear();
        }

        private void RefreshCardsForFood(FoodDataSO food)
        {
            var tickets  = _handle.GetPendingTickets(food);
            int freeCount   = 0;
            bool hasOccupied = false;

            foreach (var t in tickets)
            {
                if (t.State == OrderState.Waiting) freeCount++;
                else if (t.State is OrderState.Reserved or OrderState.InProgress) hasOccupied = true;
            }

            // 미점유 카드
            if (freeCount > 0)
            {
                if (!_freeCards.TryGetValue(food, out var card))
                {
                    card = Instantiate(_cardPrefab, _content);
                    card.Setup(food, freeCount, false, OnFreeCardClicked);
                    _freeCards[food] = card;
                }
                else
                {
                    card.Refresh(freeCount, false);
                }
            }
            else if (_freeCards.TryGetValue(food, out var staleCard))
            {
                Destroy(staleCard.gameObject);
                _freeCards.Remove(food);
            }

            // 점유 카드
            if (hasOccupied)
            {
                if (!_occupiedCards.ContainsKey(food))
                {
                    var card = Instantiate(_cardPrefab, _content);
                    card.Setup(food, 0, true, OnOccupiedCardClicked);
                    _occupiedCards[food] = card;
                }
            }
            else if (_occupiedCards.TryGetValue(food, out var staleCard))
            {
                Destroy(staleCard.gameObject);
                _occupiedCards.Remove(food);
            }
        }

        // --- 클릭 핸들러 ---

        private void OnFreeCardClicked(FoodDataSO food)
        {
            var ticket = _handle.GetFreeTicket(food);
            if (ticket == null) return;
            if (!_handle.TryOccupy(ticket)) return;

            _orderChannel?.RaiseEvent(new PlayerCraftStartEvent(ticket));
            Close();
        }

        private void OnOccupiedCardClicked(FoodDataSO food)
        {
            var ticket = _handle.GetOccupiedTicket(food);
            if (ticket == null) return;

            _confirmUI.Show(
                ticket,
                onConfirm: () =>
                {
                    if (!_handle.TrySteal(ticket)) return;
                    _orderChannel?.RaiseEvent(new PlayerCraftStartEvent(ticket));
                    Close();
                },
                onCancel: () => { }
            );
        }

        // --- 이벤트 핸들러 ---

        private void OnOrderRegistered(OrderRegisteredEvent e)
        {
            if (e.Ticket.WorkPhase != OrderWorkPhase.PendingCook) return;
            RefreshCardsForFood(e.Ticket.Food);
        }

        private void OnOrderUnregistered(OrderUnregisteredEvent e)
        {
            RefreshCardsForFood(e.Ticket.Food);
        }

        private void OnOrderStateChanged(OrderStateChangedEvent e)
        {
            RefreshCardsForFood(e.Ticket.Food);
        }
    }
}
```

- [ ] **Step 2: Unity에서 컴파일 오류 없음 확인**

- [ ] **Step 3: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/UI/Order/OrderBoardCraftUI.cs"
git add "Assets/00. Work/BBJ/02. Scripts/UI/Order/OrderBoardCraftUI.cs.meta"
git commit -m "feat: add OrderBoardCraftUI"
```

---

## Task 6: Scene 세팅 및 연결

**Goal:** 위에서 만든 스크립트와 Prefab을 씬에 연결하고, 플레이 모드에서 동작을 검증한다.

- [ ] **Step 1: PlayerOrderHandle 씬 배치**

  1. 플레이어 GameObject(또는 별도 Manager GameObject)에 `PlayerOrderHandle` 컴포넌트 부착
  2. Inspector:
     - `_playerOwner` → 플레이어의 `ModuleOwner` GameObject 연결
     - `_orderChannel` → 기존 `EventChannelSO` asset 연결

- [ ] **Step 2: OrderBoardCraftUI 씬 배치**

  1. Canvas 아래에 Panel 생성 → 이름 `OrderBoardCraftPanel`
  2. `OrderBoardCraftUI` 컴포넌트 부착
  3. `StealConfirmPanel` Prefab을 자식으로 인스턴스화하고 `_confirmUI` 연결
  4. Inspector 필드 연결:
     - `_handle` → PlayerOrderHandle이 있는 GameObject
     - `_cardPrefab` → `FoodGroupCard` Prefab
     - `_confirmUI` → StealConfirmPanel 인스턴스
     - `_orderChannel` → 기존 `EventChannelSO` asset
     - `_content` → 카드들이 들어갈 `Transform` (ScrollView Content 권장)
     - `_cancelButton` → 닫기 버튼

- [ ] **Step 3: 열기 연결**

  `OrderBoardCraftUI.Open()`을 호출할 트리거(버튼, 인터랙션, 키입력 등)를 연결한다.  
  임시 테스트용으로는 빈 MonoBehaviour에서 `Update()`에 `Input.GetKeyDown(KeyCode.Tab) → boardUI.Open()` 코드를 넣어도 된다.

- [ ] **Step 4: 플레이 모드 검증**

  1. **기본 표시**: PendingCook 티켓 2개 (같은 음식) 생성 → "음식명 x2" 카드 1개만 표시되는지 확인
  2. **미점유/점유 분리**: Agent가 1개 점유 → "x1" 카드 + "점유" 카드 2개로 분리되는지 확인
  3. **미점유 클릭**: "x2" 카드 클릭 → `PlayerCraftStartEvent` 발행 확인 (Console에 Debug.Log 임시 리스너 추가), 보드 닫힘 확인
  4. **점유 클릭**: "점유" 카드 클릭 → StealConfirmUI 표시 확인
  5. **승낙**: Confirm 버튼 → `PlayerCraftStartEvent` 발행 + 보드 닫힘 확인
  6. **취소**: Cancel 버튼 → StealConfirmUI만 닫히고 보드 유지 확인
  7. **ESC**: ESC 키 → 보드 닫힘 확인

- [ ] **Step 5: 커밋**

```bash
git add "Assets/00. Work/BBJ/01. Scene/Main.unity"
git commit -m "feat: wire OrderBoardCraftUI in scene"
```

---

## Self-Review 체크

- [x] `PlayerCraftStartEvent` Task 1에서 정의, Task 5에서 사용 — 일치
- [x] `TryOccupy`, `TrySteal`, `GetFreeTicket`, `GetOccupiedTicket`, `GetPendingTickets`, `GetAllPendingFoods` — Task 2에서 정의, Task 5에서 모두 사용
- [x] `FoodGroupCardUI.Setup(FoodDataSO, int, bool, Action<FoodDataSO>)` + `Refresh(int, bool)` — Task 3 정의, Task 5 사용 일치
- [x] `StealConfirmUI.Show(OrderTicket, Action, Action)` + `Hide()` — Task 4 정의, Task 5 사용 일치
- [x] `OrderStateChangedEvent` — 기존 정의 사용, Task 2 `TrySteal` 내부에서 raise
- [x] Spec의 "ESC/취소 버튼 → Close()" — Task 5 `Update()` + `_cancelButton` 처리
- [x] Spec의 "점유됐으면 Close()" — 미점유 클릭, 뺏기 승낙 모두 Close() 포함
