using System.Collections.Generic;
using Agents.StatSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot
{
    [CreateAssetMenu(fileName = "UpgradeData", menuName = "SO/UpgradeSO", order = 0)]
    public class UpgradeDataSO : ScriptableObject
    {
        [field: SerializeField] public string Title { get; private set; }
        [field: SerializeField] public UpgradeType UpgradeType { get; private set; }
        [field: SerializeField] public string Description { get; private set; }
        [field: SerializeField] public List<int> Costs { get; private set; }
        [field: SerializeField] public StatSO TargetStat { get; private set; }
        [field: SerializeField] public float IncreaseValue { get; private set; }
        public int MaxLevel => Costs.Count;
    }
}