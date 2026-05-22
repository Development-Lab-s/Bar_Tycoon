using BBJ.EventSystem;
using BBJ.Order;
using BBJ.Work;
using Gamelib.EventSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBJ.UI.Order
{
    public class OrderBoardUI : MonoBehaviour
    {
        [SerializeField] private EventChannelSO _orderChannel;
        [SerializeField] private OrderTicketUI  _ticketPrefab;
        [SerializeField] private Transform      _content;

        private readonly Dictionary<OrderTicket, OrderTicketUI> _active = new();

        private void OnEnable()
        {
            _orderChannel?.AddListener<OrderRegisteredEvent>(OnRegistered);
            _orderChannel?.AddListener<OrderUnregisteredEvent>(OnUnregistered);
            _orderChannel?.AddListener<OrderStateChangedEvent>(OnStateChanged);
        }

        private void OnDisable()
        {
            _orderChannel?.RemoveListener<OrderRegisteredEvent>(OnRegistered);
            _orderChannel?.RemoveListener<OrderUnregisteredEvent>(OnUnregistered);
            _orderChannel?.RemoveListener<OrderStateChangedEvent>(OnStateChanged);
        }

        private void OnRegistered(OrderRegisteredEvent e)
        {
            if (_active.ContainsKey(e.Ticket)) return;

            var ui = Instantiate(_ticketPrefab, _content);
            ui.Setup(e.Ticket, RequestCancel);
            ui.gameObject.SetActive(false);
            _active[e.Ticket] = ui;
        }

        private void OnUnregistered(OrderUnregisteredEvent e)
        {
            if (!_active.TryGetValue(e.Ticket, out var ui)) return;
            _active.Remove(e.Ticket);
            Destroy(ui.gameObject);
        }

        private void OnStateChanged(OrderStateChangedEvent e)
        {
            if (!_active.TryGetValue(e.Ticket, out var ui)) return;

            bool show = e.Ticket.State == OrderState.InProgress;
            ui.gameObject.SetActive(show);
            if (show) ui.Refresh();
        }

        public void RequestCancel(OrderTicket ticket)
        {
            _orderChannel?.RaiseEvent(new OrderCancelRequestEvent(ticket, CancelReason.PlayerCancelled));
        }
    }
}
