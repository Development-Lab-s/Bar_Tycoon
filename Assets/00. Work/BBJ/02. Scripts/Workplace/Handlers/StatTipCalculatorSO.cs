using Agents.StatSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.WorkplaceSystem.Handlers
{
    [CreateAssetMenu(fileName = "StatTipCalculator", menuName = "Tycoon/Tip/StatTip")]
    public class StatTipCalculatorSO : TipCalculatorSO
    {
        [SerializeField] private StatSO _stat;

        public override int Calculate(ModuleOwner executor)
        {
            var statModule = executor.GetModule<IStatModule>();
            if (statModule == null) return 0;
            return statModule.TryGetStat(_stat.AssetIndex, out StatSO stat)
                ? Mathf.RoundToInt(stat.Value) : 0;
        }
    }
}
