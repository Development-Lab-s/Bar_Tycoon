using System;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.Exp.ExpDatas;
using _00._Work.Goat._02._Scripts.SaveCode;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Exp
{
    public class ExpManager : MonoBehaviour
    {
        [SerializeField] private EventChannelSO expChannelSO;
        [SerializeField] private ExpTableSO expTableSo;
        [SerializeField] private ExpData expData;
        [SerializeField] private SaveFileNameSO saveFileNameSO;
        
        public int CurrentLevel => expData.currentLevel;
        public int CurrentExp => expData.currentExp;
        
        public Action<int> OnLevelChanged;
        public Action<int, int> OnExpChanged;

        private void Awake()
        {
            expChannelSO.AddListener<ExpEvent>(HandleExpAdd);
        }

        private void Start()
        {
            OnLevelChanged?.Invoke(CurrentLevel);
            OnExpChanged?.Invoke(CurrentExp, expTableSo.GetRequiredExp(CurrentLevel));
        }

        private void OnDestroy()
        {
            expChannelSO.RemoveListener<ExpEvent>(HandleExpAdd);
        }

        private void HandleExpAdd(ExpEvent exp)
        {
            expData.currentExp += exp.amount;
            int maxExp = expTableSo.GetRequiredExp(CurrentLevel);
            if (expData.currentExp >= maxExp)
            {
                expData.currentExp -= maxExp;
                expData.currentLevel += 1;
                OnLevelChanged?.Invoke(CurrentLevel);
            }
            OnExpChanged?.Invoke(CurrentExp, maxExp);
        }
    }
}