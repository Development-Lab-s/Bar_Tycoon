using BBJ.Order;
using BBJ.Register;
using BBJ.Staff;
using BBJ.Work;
using BBJ.WorkplaceSystem;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using Gamelib.EventSystem;

namespace BBJ.Schedule
{
    public class SchedulingModule : MonoBehaviour, IModule, ISchedulable, IAfterInitModule
    {
        [SerializeField] private ScheduleRegisterSO _scheduleRegister;
        [SerializeField] private EventChannelSO     _scheduleChannel;
        [SerializeField] private InteractRoleSO     _interactRole;
        [field: SerializeField] public AgentRole Role { get; private set; }

        public InteractRoleSO InteractRole => _interactRole;

        private ModuleOwner          _owner;
        private WorkExecutionContext _execCtx;

        public WorkSO      CurrentWork   { get; private set; }
        public OrderTicket CurrentTicket { get; private set; }

        public bool IsAvailableForWork => _execCtx == null;
        public bool IsWorkPaused => _execCtx?.IsPaused ?? false;

        public event Action OnWorkStarted;
        public event Action<bool> OnWorkEnded;

        public void Initialize(ModuleOwner owner)
        {
            this._owner = owner;
            UtilDebugger.AssertAllAssigned(this);
        }
        public void AfterInit()
        {
            _scheduleRegister.Register(this);
        }

        private void OnDisable()
        {
            _scheduleRegister.Unregister(this);
            CancelWork();
        }

        public void AssignWork(WorkSO workSO, OrderTicket ticket)
        {
            CancelWork();
            CurrentWork   = workSO;
            CurrentTicket = ticket;
            _execCtx      = new WorkExecutionContext();
            RunAsync(workSO, ticket, _execCtx).Forget();
        }

        public void CancelWork()
        {
            _execCtx?.HardCancel();
            _execCtx = null;
        }

        public void Pause()  => _execCtx?.Pause();
        public void Resume() => _execCtx?.Resume();

        private async UniTaskVoid RunAsync(
            WorkSO workSO, OrderTicket ticket, WorkExecutionContext ctx)
        {
            OnWorkStarted?.Invoke();
            WorkResult result = WorkResult.Cancelled;
            try
            {
                result = await workSO.ExecuteAsync(_owner, ticket, ctx);
            }
            catch (OperationCanceledException)
            {
                result = WorkResult.Cancelled;
            }
            finally
            {
                bool isCurrentCtx = _execCtx == ctx;
                if (isCurrentCtx)
                {
                    _execCtx      = null;
                    CurrentWork   = null;
                    CurrentTicket = null;
                }
                ctx.Dispose();
                OnWorkEnded?.Invoke(result == WorkResult.Completed);
                _scheduleChannel?.RaiseEvent(new ScheduleTriggerEvent());
            }
        }
    }
}
