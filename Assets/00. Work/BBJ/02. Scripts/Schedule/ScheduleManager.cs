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
        public static ScheduleManager Instance { get; private set; }

        [SerializeField] private ScheduleRegisterSO _scheduleRegister;
        [SerializeField] private EventChannelSO     _scheduleTriggerChannel;

        private readonly Queue<PendingRequest> _pending = new();

        private void Awake() => Instance = this;

        private void OnEnable()
            => _scheduleTriggerChannel?.AddListener<ScheduleTriggerEvent>(OnAgentAvailable);

        private void OnDisable()
            => _scheduleTriggerChannel?.RemoveListener<ScheduleTriggerEvent>(OnAgentAvailable);

        public void Request(AgentRole role, WorkSO work, GameEvent context)
        {
            var agent = _scheduleRegister.FindAvailable(role);

            if (agent != null)
            {
                agent.AssignWork(work, context);
                return;
            }
            _pending.Enqueue(new PendingRequest(role, work, context));
        }

        private void OnAgentAvailable(ScheduleTriggerEvent _) => DrainQueue();

        private void DrainQueue()
        {
            int count = _pending.Count;
            while (count-- > 0)
            {
                var req   = _pending.Dequeue();
                var agent = _scheduleRegister.FindAvailable(req.Role);
                if (agent != null)
                    agent.AssignWork(req.Work, req.Context);
                else
                    _pending.Enqueue(req);
            }
        }

        private readonly struct PendingRequest
        {
            public readonly AgentRole  Role;
            public readonly WorkSO     Work;
            public readonly GameEvent  Context;

            public PendingRequest(AgentRole role, WorkSO work, GameEvent context)
            {
                Role    = role;
                Work    = work;
                Context = context;
            }
        }
    }

    public class ScheduleTriggerEvent : GameEvent { }
}
