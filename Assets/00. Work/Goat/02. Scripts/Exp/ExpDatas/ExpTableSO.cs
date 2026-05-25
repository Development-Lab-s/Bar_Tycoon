using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Exp.ExpDatas
{
    [CreateAssetMenu(fileName = "ExpTable", menuName = "SO/expTable", order = 0)]
    public class ExpTableSO : ScriptableObject
    {
        [SerializeField] private List<int> requiredExpByLevel = new();
        
        public int LevelUpCount => requiredExpByLevel.Count;

        public int MaxLevel => requiredExpByLevel.Count + 1;

        public int GetRequiredExp(int level)
        {
            int index = level - 1;

            if (index < 0 || index >= requiredExpByLevel.Count)
                return 0;

            return requiredExpByLevel[index];
        }
    }
}