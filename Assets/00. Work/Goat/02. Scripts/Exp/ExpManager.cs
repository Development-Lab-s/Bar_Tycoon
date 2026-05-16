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
        [field: SerializeField] public ExpTableSO ExpTableSo { get; private set; }
        [SerializeField] private EventChannelSO expChannelSO;
        [SerializeField] private ExpData expData;
        [SerializeField] private SaveFileNameSO saveFileNameSO;
        
        public int CurrentLevel => expData.currentLevel;
        public int CurrentExp => expData.currentExp;
        
        public Action<int, int> OnLevelChanged;
        public Action<int, int, int> OnExpChanged;

        private JsonSaveService _jsonSaveService;
        private void Awake()
        {
            _jsonSaveService = new JsonSaveService(saveFileNameSO);
            LoadExp();
            
            expChannelSO.AddListener<ExpEvent>(HandleExpAdd);
        }

        private void OnDestroy()
        {
            expChannelSO.RemoveListener<ExpEvent>(HandleExpAdd);
        }
        
        private void LoadExp()
        {
            expData = _jsonSaveService.Load<ExpData>();

            if (expData == null)
                expData = new ExpData();
        }

        private void HandleExpAdd(ExpEvent exp)
        {
            int beforeLevel = CurrentLevel;

            expData.currentExp += exp.amount;

            int levelUpCount = 0;
            while (CanLevelUp())
            {
                int maxExp = ExpTableSo.GetRequiredExp(CurrentLevel);
                
                expData.currentExp -= maxExp;
                expData.currentLevel += 1;
                levelUpCount++;
            }
            
            int afterLevel = CurrentLevel;

            if (beforeLevel != afterLevel)
            {
                OnLevelChanged?.Invoke(afterLevel, beforeLevel);
            }
            
            OnExpChanged?.Invoke(levelUpCount,CurrentExp,  ExpTableSo.GetRequiredExp(CurrentLevel));
            _jsonSaveService.Save(expData);
        }
        
        private bool CanLevelUp()
        {
            int requiredExp = ExpTableSo.GetRequiredExp(CurrentLevel);

            if (requiredExp <= 0)
                return false;

            return expData.currentExp >= requiredExp;
        }
    }
}