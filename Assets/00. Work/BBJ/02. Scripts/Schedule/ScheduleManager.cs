using BBJ.Agents;
using BBJ.Register;
using BBJ.Schedule;
using BBJ.Tycoon;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Collections.Generic;
using UnityEngine;

namespace BBJ.Schedule
{
    /// <summary>
    /// 직원에게 일을 배정하는 매니저.
    ///
    /// 변경:
    ///   - WorkerConfigHolder 제거 → WorkerAgent.Config 직접 참조
    ///   - FindNearestByPathCost 전 Euclidean 거리 1차 필터로 경로 요청 수 제한
    ///     (후보 전체에 경로 요청하던 기존 방식 → 거리 상위 N개만 요청)
    /// </summary>
    public class ScheduleManager : MonoBehaviour
    {
        [SerializeField] private EventChannelSO      _scheduleTriggerChannel;
        [SerializeField] private WorkplaceRegisterSO _workplaceRegister;
        [SerializeField] private ScheduleRegisterSO  _scheduleRegister;
        [SerializeField] private PathRequestSO       _pathRequestSO;

        /// <summary>
        /// Euclidean 1차 필터 후 경로 요청할 최대 후보 수.
        /// 직원/Workplace 규모가 커지면 이 값을 조정한다.
        /// </summary>
        [SerializeField] private int _pathCandidateLimit = 3;

        private void OnEnable()
            => _scheduleTriggerChannel.AddListener<ScheduleTriggerEvent>(OnScheduleTrigger);

        private void OnDisable()
            => _scheduleTriggerChannel.RemoveListener<ScheduleTriggerEvent>(OnScheduleTrigger);

        private void OnScheduleTrigger(ScheduleTriggerEvent _) => RunScheduleAsync().Forget();

        public void RunSchedule() => RunScheduleAsync().Forget();

        private async UniTaskVoid RunScheduleAsync()
        {
            foreach (var schedulable in _scheduleRegister.Agents)
            {
                if (schedulable.IsWorking) continue;

                // WorkerAgent에서 Config를 직접 가져온다 (WorkerConfigHolder 제거)
                var workerAgent = (schedulable as BBJ.Modules.SchedulingModule)
                    ?.GetComponent<WorkerAgent>();

                if (workerAgent?.Config == null) continue;

                foreach (var workSO in workerAgent.Config.PriorityWorks)
                {
                    // 1차: WorkplaceRegisterSO에서 타입 + IsOccupied 필터 (Euclidean 거리순)
                    var candidates = _workplaceRegister.GetCandidates(
                        schedulable.transform.position,
                        workSO.RequiredWorkplaceType,
                        _pathCandidateLimit); // ← 거리 상위 N개로 제한

                    if (candidates.Count == 0) continue;

                    // 2차: WorkSO 실행 가능 조건 필터
                    candidates = candidates.FindAll(w => workSO.CanExecute(w));
                    if (candidates.Count == 0) continue;

                    // 3차: 남은 후보(최대 N개)에 대해서만 경로비용 계산
                    var best = await FindNearestByPathCost(schedulable, candidates);
                    if (best == null) continue;

                    schedulable.AssignWork(best, workSO);
                    break;
                }
            }
        }

        /// <summary>
        /// 후보 목록(이미 거리 필터 완료)에서 실제 경로 길이 기준 최적 Workplace 선택.
        /// 입력이 최대 _pathCandidateLimit개이므로 경로 요청 횟수가 제한된다.
        /// </summary>
        private async UniTask<Workplace> FindNearestByPathCost(
            ISchedulable schedulable, List<Workplace> candidates)
        {
            Workplace best     = null;
            int       bestCost = int.MaxValue;

            foreach (var workplace in candidates)
            {
                var destination = workplace.GetNearestPoint(schedulable.transform.position);
                var tcs = new UniTaskCompletionSource<(Vector3[] path, bool success)>();

                _pathRequestSO.RequestPath(
                    schedulable.transform.position,
                    destination,
                    (path, success) => tcs.TrySetResult((path, success)));

                var (path, success) = await tcs.Task;
                if (!success) continue;

                if (path.Length < bestCost)
                {
                    bestCost = path.Length;
                    best     = workplace;
                }
            }

            return best;
        }
    }

    public class ScheduleTriggerEvent : GameEvent { }
}
