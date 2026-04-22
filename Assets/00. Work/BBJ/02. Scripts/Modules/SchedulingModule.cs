using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Agents.FSM;
using BBJ.Register;
using BBJ.Schedule;
using BBJ.Tycoon;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using UnityEngine;

namespace BBJ.Modules
{
    public class SchedulingModule : MonoBehaviour, IModule, ISchedulable
    {
        [SerializeField] private ScheduleRegisterSO _scheduleRegister;
        [SerializeField] private PathRequestSO      _pathRequestSO;

        private ModuleOwner        _owner;
        private PathMovementModule _movement;

        public Workplace AssignedWorkplace { get; private set; }
        public bool IsWorking => AssignedWorkplace != null;

        public event Action OnWorkStarted;
        public event Action OnWorkEnded;

        /// <summary>
        /// WorkerAgent가 구독해 FSM 상태를 전환한다.
        /// SchedulingModule은 FSM을 직접 참조하지 않는다.
        /// </summary>
        public event Action<WorkerState> OnStateChangeRequested;

        public void Initialize(ModuleOwner owner)
        {
            _owner    = owner;
            _movement = owner.GetModule<PathMovementModule>();
            _scheduleRegister?.Register(this);
        }

        private void OnDestroy() => _scheduleRegister?.Unregister(this);

        public void AssignWork(Workplace workplace, WorkSO workSO)
        {
            AssignedWorkplace = workplace;
            AssignedWorkplace.Occupy();
            OnWorkStarted?.Invoke();

            (_owner as MonoBehaviour)?.StartCoroutine(
                RunWork(workSO.Execute(_owner, workplace)));
        }

        public void CompleteWork()
        {
            AssignedWorkplace?.Release();
            AssignedWorkplace = null;
            OnWorkEnded?.Invoke();
            OnStateChangeRequested?.Invoke(WorkerState.Idle);
        }

        private IEnumerator RunWork(IEnumerator workRoutine)
        {
            while (workRoutine.MoveNext())
            {
                var current = workRoutine.Current;

                if (current is MoveStep move)
                {
                    OnStateChangeRequested?.Invoke(WorkerState.Move);
                    yield return ExecuteMove(move.Destination).ToCoroutine();
                }
                else if (current is WaitUntilStep waitUntil)
                {
                    OnStateChangeRequested?.Invoke(WorkerState.Wait);
                    yield return new WaitUntil(waitUntil.Condition);
                }
                else if (current is WaitStep wait)
                {
                    OnStateChangeRequested?.Invoke(WorkerState.Wait);
                    yield return new WaitForSeconds(wait.Seconds);
                }
                else if (current is WorkActionStep work)
                {
                    OnStateChangeRequested?.Invoke(WorkerState.Work);
                    yield return new WaitForSeconds(work.Seconds);
                }
                else
                {
                    yield return current;
                }
            }

            // WorkSO.Execute()가 완전히 끝난 뒤 완료 처리.
            // WorkSO는 CompleteWork()를 호출하지 않는다.
            CompleteWork();
        }

        private async UniTask ExecuteMove(Vector3 destination)
        {
            var arrived = false;

            void OnArrived() => arrived = true;
            _movement.MoveComplectedEvent += OnArrived;

            _pathRequestSO.RequestPath(
                _owner.transform.position,
                destination,
                (path, success) =>
                {
                    if (success && path.Length > 0) _movement.OnPathMove(path);
                    else arrived = true;
                });

            await UniTask.WaitUntil(() => arrived);
            _movement.MoveComplectedEvent -= OnArrived;
        }
    }
}
