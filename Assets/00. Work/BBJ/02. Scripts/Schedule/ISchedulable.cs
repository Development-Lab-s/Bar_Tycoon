using BBJ.Tycoon;
using System;
using UnityEngine;

namespace BBJ.Schedule
{
    /// <summary>
    /// ScheduleManager가 알아야 하는 직원 모듈의 최소 계약.
    ///
    /// 변경: AssignWork(Workplace) 단독 오버로드 제거.
    ///   실제 사용처가 없고, SchedulingModule의 빈 구현을 인터페이스가 강제하는 구조였음.
    ///   WorkSO 없이 Workplace만 배정하는 시나리오가 생기면 그때 추가한다.
    /// </summary>
    public interface ISchedulable
    {
        Transform transform { get; }
        bool      IsWorking { get; }

        event Action OnWorkStarted;
        event Action OnWorkEnded;

        void AssignWork(Workplace workplace, WorkSO workSO);
        void CompleteWork();
    }
}
