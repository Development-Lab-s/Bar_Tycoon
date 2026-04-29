using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.AchievementUI.Data
{
    [CreateAssetMenu(fileName = "AchieveData", menuName = "SO/achievement", order = 0)]
    public class AchievementDataSO : ScriptableObject
    {
        [field: SerializeField] public AchievementType AchievementType { get; private set; }
        [field: SerializeField] public string AchievementName { get; private set; }
        [field: SerializeField] public string AchievementDescription { get; private set; }
        [field: SerializeField] public int TargetAchievementDegree { get; private set; }
    }
}