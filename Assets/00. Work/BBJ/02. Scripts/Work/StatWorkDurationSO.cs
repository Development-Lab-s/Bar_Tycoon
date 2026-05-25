using _00._Work._Resources._02._Scripts.Agents.StatSystem;
using _00._Work._Resources._02._Scripts.Modules;
using Agents.StatSystem;
using BBJ.Order;
using BBJ.WorkplaceSystem.Modules;
using UnityEngine;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "StatWorkDuration", menuName = "Tycoon/WorkDuration/Stat")]
    public class StatWorkDurationSO : WorkDurationSO
    {
        [SerializeField] private StatSO _stat;
        [SerializeField] private float  _baseDuration = 3f;    // stat적용이 없을 때 시간
        [SerializeField] private float  _minDuration  = 0.5f;  // 최단 속력


        public override float GetDuration(IStatModule workerStat, OrderTicket conductData)
        {
            float statValue = GetStatValue(workerStat);

            return Mathf.Max(_minDuration, _baseDuration / (1f + statValue));
        }

        private float GetStatValue(IStatModule workerStat)
        {
            float statValue = default;
            if (workerStat.TryGetStat(_stat.AssetIndex, out StatSO stat))
                statValue = stat.Value;

            return statValue;
        }
    }
}
