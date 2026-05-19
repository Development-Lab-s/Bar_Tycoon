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
            UtilDebugger.AssertAllAssigned(this);

            if (_confirmButton != null) _confirmButton.onClick.AddListener(OnConfirm);
            if (_cancelButton  != null) _cancelButton.onClick.AddListener(OnCancel);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_confirmButton != null) _confirmButton.onClick.RemoveListener(OnConfirm);
            if (_cancelButton  != null) _cancelButton.onClick.RemoveListener(OnCancel);
        }

        public void Show(OrderTicket ticket, Action onConfirm, Action onCancel)
        {
            if (ticket == null) return;

            _messageLabel.text = $"[{ticket.Ordered?.cocktailName}] 현재 제작 중입니다.\n작업을 빼앗겠습니까?";
            _onConfirm = onConfirm;
            _onCancel  = onCancel;
            gameObject.SetActive(true);
        }

        public void ShowFailed(string message)
        {
            _messageLabel.text = message;
            _confirmButton.gameObject.SetActive(false);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            if (_confirmButton != null)
                _confirmButton.gameObject.SetActive(true);
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
