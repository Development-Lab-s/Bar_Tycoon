using BBJ.Order;
using BBJ.Register;
using BBJ.Staff;
using BBJ.Work;
using Gamelib.EventSystem;
using System.Collections.Generic;
using UnityEngine;

namespace BBJ.Schedule
{
    public class ScheduleManager : MonoBehaviour
    {
        [SerializeField] private ScheduleRegisterSO _scheduleRegister;
        [SerializeField] private EventChannelSO     _scheduleChannel;

        private readonly Queue<PendingRequest> _pending = new();
        private bool _isDraining;

        private void Awake()
        {
            UtilDebugger.AssertAllAssigned(this);
            SubEventChannel();
        }

        private void OnDestroy()
        {
            UnsubEventChannel();
        }

        public void Request(AgentRole role, WorkSO work, OrderTicket ticket)
        {
            var agent = _scheduleRegister.FindAvailable(role);
            if (agent != null)
            {
                agent.AssignWork(work, ticket);
                return;
            }
            _pending.Enqueue(new PendingRequest(role, work, ticket));
        }
        private void DrainQueue()
        {
            if (_isDraining) return;
            _isDraining = true;

            int count = _pending.Count;
            while (count-- > 0)
            {
                var req   = _pending.Dequeue();
                var agent = _scheduleRegister.FindAvailable(req.Role);
                if (agent != null)
                    agent.AssignWork(req.Work, req.Ticket);
                else
                    _pending.Enqueue(req);
            }

            _isDraining = false;
        }
        private void SubEventChannel()
        {
            _scheduleChannel.AddListener<ScheduleTriggerEvent>(HandleAgentAvailable);
            _scheduleChannel.AddListener<ScheduleRequestEvent>(HandleScheduleRequested);
        }
        private void UnsubEventChannel()
        {
            _scheduleChannel.RemoveListener<ScheduleTriggerEvent>(HandleAgentAvailable);
            _scheduleChannel.RemoveListener<ScheduleRequestEvent>(HandleScheduleRequested);
        }
        private void HandleScheduleRequested(ScheduleRequestEvent e) => Request(e.Role, e.Work, e.Ticket);
        private void HandleAgentAvailable(ScheduleTriggerEvent _) => DrainQueue();

        private readonly struct PendingRequest
        {
            public readonly AgentRole    Role;
            public readonly WorkSO       Work;
            public readonly OrderTicket  Ticket;

            public PendingRequest(AgentRole role, WorkSO work, OrderTicket ticket)
            {
                Role   = role;
                Work   = work;
                Ticket = ticket;
            }
        }
    }

    public class ScheduleTriggerEvent : GameEvent { }
}
