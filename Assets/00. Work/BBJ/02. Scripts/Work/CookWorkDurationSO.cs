using Agents.StatSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "CookWorkDuration", menuName = "Tycoon/WorkDuration/Cook")]
    public class CookWorkDurationSO : WorkDurationSO
    {
        [SerializeField] private StatSO _cookStat;
        [SerializeField] private float  _minDuration = 0.5f;

        public override float GetDuration(ModuleOwner worker)
        {
            float baseDuration = GetBaseCookTime(worker);
            float statValue    = GetStatValue(worker);
            return Mathf.Max(_minDuration, baseDuration / (1f + statValue));
        }

        private float GetBaseCookTime(ModuleOwner worker)
        {
            var provider = worker.GetModule<ICurrentFoodProvider>();
            if (provider?.CurrentFood != null)
                return provider.CurrentFood.CookTime;
            return _minDuration;
        }

        private float GetStatValue(ModuleOwner worker)
        {
            var statModule = worker.GetModule<IStatModule>();
            return statModule.TryGetStat(_cookStat.AssetIndex, out StatSO stat) ? stat.Value : 0f;
        }
    }
}
