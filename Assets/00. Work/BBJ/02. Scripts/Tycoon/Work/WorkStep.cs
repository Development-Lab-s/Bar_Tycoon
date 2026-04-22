using System;
using UnityEngine;

namespace BBJ.Tycoon
{
    /// <summary>
    /// WorkSO.Execute()에서 yield return하는 스텝 베이스.
    /// SchedulingModule.RunWork()가 타입을 보고 StateMachine 상태를 전환하고 실행한다.
    /// </summary>
    public abstract class WorkStep { }

    /// <summary>목적지까지 이동</summary>
    public sealed class MoveStep : WorkStep
    {
        public readonly Vector3 Destination;
        public MoveStep(Vector3 destination) => Destination = destination;
    }

    /// <summary>지정 시간 대기</summary>
    public sealed class WaitStep : WorkStep
    {
        public readonly float Seconds;
        public WaitStep(float seconds) => Seconds = seconds;
    }

    /// <summary>조건 충족까지 대기</summary>
    public sealed class WaitUntilStep : WorkStep
    {
        public readonly Func<bool> Condition;
        public WaitUntilStep(Func<bool> condition) => Condition = condition;
    }

    /// <summary>작업 수행(서빙·조리 등)</summary>
    public sealed class WorkActionStep : WorkStep
    {
        public readonly float Seconds;
        public WorkActionStep(float seconds) => Seconds = seconds;
    }
}
