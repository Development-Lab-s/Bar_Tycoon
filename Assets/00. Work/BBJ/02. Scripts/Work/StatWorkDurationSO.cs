using Agents.StatSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "StatWorkDuration", menuName = "Tycoon/WorkDuration/Stat")]
    public class StatWorkDurationSO : WorkDurationSO
    {
        [SerializeField] private StatSO _stat;
        [SerializeField] private float  _baseDuration = 3f;
        [SerializeField] private float  _minDuration  = 0.5f;

        public override float GetDuration(ModuleOwner worker)
        {
            float statValue = GetStatValue(worker);
            return Mathf.Max(_minDuration, _baseDuration / (1f + statValue));
        }

        private float GetStatValue(ModuleOwner worker)
        {
            var statModule = worker.GetModule<IStatModule>();
            return statModule.TryGetStat(_stat.AssetIndex, out StatSO stat) ? stat.Value : 0f;
        }
    }
}
