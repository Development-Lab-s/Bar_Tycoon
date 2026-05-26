using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Exp.ExpDatas
{
    [CreateAssetMenu(fileName = "ExpTable", menuName = "SO/expTable", order = 0)]
    public class ExpTableSO : ScriptableObject
    {
        [SerializeField] private List<int> requiredExpByLevel = new();
        [SerializeField] private int requiredLevelUp = 100;
        
        public int LevelUpCount => requiredExpByLevel.Count;

        public int MaxLevel => requiredExpByLevel.Count + 1;

        [ContextMenu("LevelUpBalance")]
        public void LevelUpBalance()
        {
            requiredExpByLevel[0] = requiredLevelUp; 
            for (int i = 1; i < requiredExpByLevel.Count; i++)
            {
                requiredExpByLevel[i] = requiredLevelUp + requiredExpByLevel[i - 1];
            }
        }

        public int GetRequiredExp(int level)
        {
            int index = level - 1;

            if (index < 0 || index >= requiredExpByLevel.Count)
                return 0;

            return requiredExpByLevel[index];
        }
    }
}