using _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex.Data;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI
{
    [CreateAssetMenu(fileName = "LevelUpReward", menuName = "SO/LevelUpReward", order = 0)]
    public class LevelUpRewardSO : ScriptableObject
    {
        public CockTailSlotSo[] cockTails;
        //기능 추가는 아직 안함
    }
}