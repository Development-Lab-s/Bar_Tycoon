using Agents.StatSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot
{
    [CreateAssetMenu(fileName = "UpgradeData", menuName = "SO/UpgradeSO", order = 0)]
    public class UpgradeDataSO : ScriptableObject
    {
        [field: SerializeField] public string      Title       { get; private set; }
        [field: SerializeField] public UpgradeType UpgradeType { get; private set; }
        [field: SerializeField] public string      Description { get; private set; }
        [field: SerializeField] public StatSO      TargetStat  { get; private set; }
        [field: SerializeField] public float       IncreaseValue { get; private set; }

        [SerializeField] private int   _maxLevel      = 100;
        [SerializeField] private int   _baseCost      = 100;
        [SerializeField] private float _costMultiplier = 1.15f;

        public int MaxLevel => _maxLevel;

        public long GetCost(int level)
        {
            return (long)Mathf.Round(_baseCost * Mathf.Pow(_costMultiplier, level));
        }
    }
}
