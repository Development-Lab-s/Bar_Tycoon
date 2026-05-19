using BBJ.Order;
using BBJ.Work;
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
        [SerializeField] private TMP_Text _priceLabel;
        //[SerializeField] private TMP_Text _seatLabel;
        //[SerializeField] private Button _cancelButton;

        private OrderTicket _ticket;
        private Action<OrderTicket> _onCancel;
        private void OnEnable() { }

        private void OnDisable() { }

        public void Setup(OrderTicket ticket, Action<OrderTicket> onCancel)
        {
            _ticket = ticket;
            _onCancel = onCancel;
            var ordered = ticket.Ordered;

            if (ordered != null)
            {
                _foodIcon.sprite = ordered.cocktailIcon;
                _foodName.text   = ordered.cocktailName ?? "-";
                _priceLabel.text = "가격 : " + ordered.price + " 원";
            }

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
                _workPhase.text = "(" + StateColor(_ticket.WorkPhase) + "...)";

            //bool canCancel = _ticket.State is not (OrderState.Done or OrderState.Cancelled);
            //_cancelButton?.gameObject.SetActive(canCancel);
        }

        private void OnCancelClicked() => _onCancel?.Invoke(_ticket);

        private static string StateColor(OrderWorkPhase state) => state switch
        {
            OrderWorkPhase.PendingCook => "조리 중",
            OrderWorkPhase.ReadyForServe => "서빙 중",
            OrderWorkPhase.Eating => "식사 중",
            OrderWorkPhase.ReadyForCashier => "계산 중",
            OrderWorkPhase.Done => "완료",
            _ => "NaN"
        };
    }
}
