using BBJ.Register;
using BBJ.Schedule;
using BBJ.Staff;
using BBJ.Work;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Schedule
{
    public class SchedulingModule : MonoBehaviour, IModule, ISchedulable, IScheduleTriggerSource, IAfterInitModule
    {
        [SerializeField] private ScheduleRegisterSO _scheduleRegister;
        [field: SerializeField] public EventChannelSO ScheduleTriggerChannel { get; private set; }
        [field: SerializeField] public AgentRole Role { get; private set; }

        private ModuleOwner          _owner;
        private WorkExecutionContext _execCtx;

        public bool IsAvailableForWork => _execCtx == null;

        public event Action OnWorkStarted;
        public event Action<bool> OnWorkEnded;

        public void Initialize(ModuleOwner owner) => _owner = owner;

        private void OnDisable()
        {
            _scheduleRegister?.Unregister(this);
            CancelWork();
        }

        public void AfterInit() => _scheduleRegister?.Register(this);

        public void AssignWork(WorkSO workSO, GameEvent context)
        {
            CancelWork();
            _execCtx = new WorkExecutionContext();
            RunAsync(workSO, context, _execCtx).Forget();
        }

        public void ResolveWork()
        {
            _execCtx?.ForceComplete();
        }

        public void CancelWork()
        {
            _execCtx?.HardCancel();
            _execCtx = null;
        }

        private async UniTaskVoid RunAsync(WorkSO workSO, GameEvent context, WorkExecutionContext ctx)
        {
            OnWorkStarted?.Invoke();
            WorkResult result = WorkResult.Cancelled;
            try
            {
                result = await workSO.ExecuteAsync(_owner, context, ctx);
            }
            catch (OperationCanceledException)
            {
                result = ctx.WasExternallyCompleted
                    ? WorkResult.ExternallyCompleted
                    : WorkResult.Cancelled;
            }
            finally
            {
                if (_execCtx == ctx) _execCtx = null;
                ctx.Dispose();
                workSO.OnResult(result, _owner, context);
                OnWorkEnded?.Invoke(result != WorkResult.Cancelled);
                ScheduleTriggerChannel?.RaiseEvent(new ScheduleTriggerEvent());
            }
        }
    }
}
