using _00._Work.Lusaload._02._Scripts.SO;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI
{
    [CreateAssetMenu(fileName = "LevelUpReward", menuName = "SO/LevelUpReward", order = 0)]
    public class LevelUpRewardSO : ScriptableObject
    {
        public CocktailRecipeSO[] cockTails;
    }
}