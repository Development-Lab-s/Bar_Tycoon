using BBJ.EventSystem;
using BBJ.Order;
using BBJ.Register;
using BBJ.Work;
using Gamelib.EventSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBJ.UI.Order
{
    public class OrderBoardUI : MonoBehaviour
    {
        [SerializeField] private EventChannelSO  _orderChannel;
        [SerializeField] private EventChannelSO  _sceneChannel;
        [SerializeField] private OrderRegisterSO _orderRegister;
        [SerializeField] private OrderTicketUI   _ticketPrefab;
        [SerializeField] private Transform       _content;

        private readonly Dictionary<OrderTicket, OrderTicketUI> _active = new();

        private void OnEnable()
        {
            _orderChannel?.AddListener<OrderRegisteredEvent>(OnRegistered);
            _orderChannel?.AddListener<OrderUnregisteredEvent>(OnUnregistered);
            _orderChannel?.AddListener<OrderStateChangedEvent>(OnStateChanged);
            _sceneChannel?.AddListener<SceneReadyEvent>(OnSceneReady);
            InitFromRegister();
        }

        private void OnDisable()
        {
            _orderChannel?.RemoveListener<OrderRegisteredEvent>(OnRegistered);
            _orderChannel?.RemoveListener<OrderUnregisteredEvent>(OnUnregistered);
            _orderChannel?.RemoveListener<OrderStateChangedEvent>(OnStateChanged);
            _sceneChannel?.RemoveListener<SceneReadyEvent>(OnSceneReady);

            foreach (var ui in _active.Values)
                if (ui != null) Destroy(ui.gameObject);
            _active.Clear();
        }

        private void OnSceneReady(SceneReadyEvent _) => InitFromRegister();

        private void InitFromRegister()
        {
            if (_orderRegister == null) return;
            foreach (var ticket in _orderRegister.Registry)
            {
                if (ticket.IsTerminal) continue;
                if (ticket.WorkPhase == OrderWorkPhase.ReadyForServer) continue;
                if (!_active.ContainsKey(ticket))
                    SpawnTicketUI(ticket);
                _active[ticket].Refresh();
            }
        }

        private void OnRegistered(OrderRegisteredEvent e)
        {
            // ReadyForServer는 아직 주문 접수 전 — 서버/플레이어가 받은 뒤 PendingCook로 전환될 때 표시
            if (e.Ticket.WorkPhase == OrderWorkPhase.ReadyForServer) return;
            SpawnTicketUI(e.Ticket);
        }

        private void OnUnregistered(OrderUnregisteredEvent e)
        {
            if (!_active.TryGetValue(e.Ticket, out var ui)) return;
            _active.Remove(e.Ticket);
            Destroy(ui.gameObject);
        }

        private void OnStateChanged(OrderStateChangedEvent e)
        {
            if (_active.TryGetValue(e.Ticket, out var ui))
            {
                ui.Refresh();
                return;
            }
            // ReadyForServer → PendingCook 전환 시 새로 등장
            if (e.Ticket.WorkPhase != OrderWorkPhase.ReadyForServer && !e.Ticket.IsTerminal)
                SpawnTicketUI(e.Ticket);
        }

        private void SpawnTicketUI(OrderTicket ticket)
        {
            if (_active.ContainsKey(ticket)) return;
            var ui = Instantiate(_ticketPrefab, _content);
            ui.Setup(ticket, RequestCancel);
            _active[ticket] = ui;
            ui.Refresh();
        }

        public void RequestCancel(OrderTicket ticket)
        {
            _orderChannel?.RaiseEvent(new OrderCancelRequestEvent(ticket, CancelReason.PlayerCancelled));
        }
    }
}