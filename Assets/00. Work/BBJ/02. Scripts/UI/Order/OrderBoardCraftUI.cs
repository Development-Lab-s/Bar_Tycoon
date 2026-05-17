using BBJ;
using _00._Work.Lusaload._02._Scripts.SO;
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

        private readonly Dictionary<CocktailRecipeSO, FoodGroupCardUI> _freeCards     = new();
        private readonly Dictionary<CocktailRecipeSO, FoodGroupCardUI> _occupiedCards = new();

        private void Awake()
        {
            UtilDebugger.AssertAllAssigned(this);
            if (_cancelButton != null) _cancelButton.onClick.AddListener(Close);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_cancelButton != null) _cancelButton.onClick.RemoveListener(Close);
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
            _confirmUI?.Hide();
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
            foreach (var card in _freeCards.Values)    Destroy(card.gameObject);
            foreach (var card in _occupiedCards.Values) Destroy(card.gameObject);
            _freeCards.Clear();
            _occupiedCards.Clear();
        }

        private void RefreshCardsForFood(CocktailRecipeSO food)
        {
            var tickets    = _handle.GetPendingTickets(food);
            int freeCount  = 0;
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

        private void OnFreeCardClicked(CocktailRecipeSO food)
        {
            var ticket = _handle.GetFreeTicket(food);
            if (ticket == null) return;
            if (!_handle.TryOccupy(ticket)) return;

            _orderChannel?.RaiseEvent(new PlayerCraftStartEvent(ticket));
            Close();
        }

        private void OnOccupiedCardClicked(CocktailRecipeSO food)
        {
            var ticket = _handle.GetOccupiedTicket(food);
            if (ticket == null) return;

            _confirmUI?.Show(
                ticket,
                onConfirm: () =>
                {
                    if (_handle.TrySteal(ticket))
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
            RefreshCardsForFood(e.Ticket.Ordered);
        }

        private void OnOrderUnregistered(OrderUnregisteredEvent e)
        {
            RefreshCardsForFood(e.Ticket.Ordered);
        }

        private void OnOrderStateChanged(OrderStateChangedEvent e)
        {
            RefreshCardsForFood(e.Ticket.Ordered);
        }
    }
}
