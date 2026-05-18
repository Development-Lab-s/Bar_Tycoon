using _00._Work.Lusaload._02._Scripts.SO;
using BBJ.EventSystem;
using BBJ.Order;
using Gamelib.EventSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Systems;

namespace BBJ.UI.Order
{
    public class OrderBoardCraftUI : MonoBehaviour
    {
        [SerializeField] private PlayerOrderHandle _handle;
        [SerializeField] private FoodGroupCardUI _cardPrefab;
        [SerializeField] private StealConfirmUI _confirmUI;
        [SerializeField] private EventChannelSO _orderChannel;
        [SerializeField] private EventChannelSO _uiChannel;
        [SerializeField] private Transform _content;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private PlayerInputSO playerInputSO;

        private readonly Dictionary<CocktailRecipeSO, FoodGroupCardUI> _freeCards = new();
        private readonly Dictionary<CocktailRecipeSO, FoodGroupCardUI> _occupiedCards = new();

        private void Awake()
        {
            UtilDebugger.AssertAllAssigned(this);

            _cancelButton.onClick.AddListener(Close);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _cancelButton.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            playerInputSO.DownPopupClick += Close;

            gameObject.SetActive(true);
            SubscribeEvents();
            RebuildAllCards();
        }

        public void Close()
        {
            playerInputSO.DownPopupClick -= Close;

            _confirmUI.Hide();
            UnsubscribeEvents();
            ClearAllCards();
            gameObject.SetActive(false);
        }

        // --- 이벤트 구독 ---

        private void SubscribeEvents()
        {
            _orderChannel.AddListener<OrderRegisteredEvent>(HandleOrderRegistered);
            _orderChannel.AddListener<OrderUnregisteredEvent>(HandleOrderUnregistered);
            _orderChannel.AddListener<OrderStateChangedEvent>(HandleOrderStateChanged);
        }

        private void UnsubscribeEvents()
        {
            _orderChannel.RemoveListener<OrderRegisteredEvent>(HandleOrderRegistered);
            _orderChannel.RemoveListener<OrderUnregisteredEvent>(HandleOrderUnregistered);
            _orderChannel.RemoveListener<OrderStateChangedEvent>(HandleOrderStateChanged);
        }

        private void RebuildAllCards()
        {
            ClearAllCards();

            var foodsAll = _handle.GetAllPendingFoods();
            foreach (var food in foodsAll) RefreshCardsForFood(food);
        }

        private void ClearAllCards()
        {
            foreach (var card in _freeCards.Values) Destroy(card.gameObject);
            foreach (var card in _occupiedCards.Values) Destroy(card.gameObject);

            _freeCards.Clear();
            _occupiedCards.Clear();
        }

        private void RefreshCardsForFood(CocktailRecipeSO food)
        {
            var tickets = _handle.GetPendingTickets(food);
            int freeCount = 0;
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

        private void OnFreeCardClicked(CocktailRecipeSO food)
        {
            var ticket = _handle.GetFreeTicket(food);
            if (ticket == null) return;

            if (_handle.TryOccupy(ticket))
            {
                _orderChannel.RaiseEvent(new PlayerCraftStartEvent(ticket));
                Close();
            }
        }

        private void OnOccupiedCardClicked(CocktailRecipeSO food)
        {
            var ticket = _handle.GetOccupiedTicket(food);
            if (ticket == null) return;

            _confirmUI.Show(ticket, onConfirm, null);

            void onConfirm()
            {
                if (_handle.TrySteal(ticket))
                    _orderChannel.RaiseEvent(new PlayerCraftStartEvent(ticket));
                else
                    _uiChannel.RaiseEvent(new MessageEvent($"[{ticket.Ordered?.cocktailName}] 작업이 이미 완료되어 빼앗을 수 없습니다."));

                Close();
            };
        }

        private void HandleOrderRegistered(OrderRegisteredEvent e)
        {
            if (e.Ticket.WorkPhase != OrderWorkPhase.PendingCook) return;
            RefreshCardsForFood(e.Ticket.Ordered);
        }

        private void HandleOrderUnregistered(OrderUnregisteredEvent e) => RefreshCardsForFood(e.Ticket.Ordered);
        private void HandleOrderStateChanged(OrderStateChangedEvent e) => RefreshCardsForFood(e.Ticket.Ordered);
    }
}
