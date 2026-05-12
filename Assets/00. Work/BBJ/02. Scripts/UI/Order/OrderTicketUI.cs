using BBJ.Order;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BBJ.UI.Order
{
    public class OrderTicketUI : MonoBehaviour
    {
        [SerializeField] private Image _foodIcon;
        [SerializeField] private TMP_Text _foodName;
        [SerializeField] private TMP_Text _stateBadge;
        [SerializeField] private TMP_Text _workPhase;
        [SerializeField] private TMP_Text _seatLabel;
        [SerializeField] private Button _cancelButton;

        private OrderTicket _ticket;
        private Action<OrderTicket> _onCancel;
        public void Setup(OrderTicket ticket, Action<OrderTicket> onCancel)
        {
            _ticket = ticket;
            _onCancel = onCancel;

            _foodIcon.sprite = ticket.Food?.Icon;
            _foodName.text = ticket.Food?.FoodName ?? "-";
            _seatLabel.text = ticket.Seat != null ? ticket.Seat.name : "-";

            _cancelButton?.onClick.AddListener(OnCancelClicked);

            Refresh();
        }

        public void Refresh()
        {
            if (_ticket == null) return;

            if (_stateBadge != null)
            {
                _stateBadge.text = _ticket.State.ToString();
            }

            if (_workPhase != null)
                _workPhase.text = _ticket.WorkPhase.ToString();

            bool canCancel = _ticket.State is not (OrderState.Done or OrderState.Cancelled);
            _cancelButton?.gameObject.SetActive(canCancel);
        }

        private void OnCancelClicked() => _onCancel?.Invoke(_ticket);

        //private static Color StateColor(OrderState state) => state switch
        //{
        //    OrderState.Waiting => Color.white,
        //    OrderState.Reserved => Color.yellow,
        //    OrderState.InProgress => Color.green,
        //    OrderState.Done => Color.cyan,
        //    OrderState.Cancelled => Color.red,
        //    _ => Color.white,
        //};
    }
}
