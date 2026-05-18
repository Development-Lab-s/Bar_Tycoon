using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI
{
    [CreateAssetMenu(fileName = "LevelUpRewards", menuName = "SO/LevelUpRewards", order = 0)]
    public class LevelUpRewardSOs : ScriptableObject
    {
        public LevelUpRewardSO[] levelUpRewardSOs;
    }
}