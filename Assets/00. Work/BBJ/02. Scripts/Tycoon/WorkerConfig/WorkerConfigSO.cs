using System.Collections.Generic;
using UnityEngine;

namespace BBJ.Tycoon
{
    [CreateAssetMenu(fileName = "WorkerConfig", menuName = "Tycoon/WorkerConfig")]
    public class WorkerConfigSO : ScriptableObject
    {
        [Tooltip("앞에 있을수록 우선 배정된다.")]
        public List<WorkSO> PriorityWorks = new();
    }
}
