using BBJ.Register;
using BBJ.Schedule;
using BBJ.Staff;
using BBJ.Work;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Schedule
{
    public class SchedulingModule : MonoBehaviour, IModule, ISchedulable, IScheduleTriggerSource, IAfterInitModule
    {
        [SerializeField] private ScheduleRegisterSO _scheduleRegister;
        [field: SerializeField] public EventChannelSO ScheduleTriggerChannel { get; private set; }
        [field: SerializeField] public AgentRole Role { get; private set; }

        private ModuleOwner             _owner;
        private CancellationTokenSource _cts;

        public bool      IsAvailableForWork => _cts == null;

        public event Action OnWorkStarted;
        public event Action OnWorkEnded;
        //public WorkSO curWorkSO;

        public void Initialize(ModuleOwner owner) => _owner = owner;

        private void OnDisable()
        {
            _scheduleRegister?.Unregister(this);
            CompleteWork();
        }

        public void AfterInit() => _scheduleRegister?.Register(this);

        public void AssignWork(WorkSO workSO, GameEvent context)
        {
            CompleteWork();
            _cts = new CancellationTokenSource();
            RunAsync(workSO, context, _cts).Forget();
        }

        public void CompleteWork()
        {
            //curWorkSO = null;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async UniTaskVoid RunAsync(WorkSO workSO, GameEvent context, CancellationTokenSource cts)
        {
            //curWorkSO = workSO;
            OnWorkStarted?.Invoke();
            try
            {
                await workSO.ExecuteAsync(_owner, context, cts.Token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (_cts == cts) _cts = null;
                //curWorkSO = null;
                cts.Dispose();
                OnWorkEnded?.Invoke();
                ScheduleTriggerChannel?.RaiseEvent(new ScheduleTriggerEvent());
            }
        }
    }
}
