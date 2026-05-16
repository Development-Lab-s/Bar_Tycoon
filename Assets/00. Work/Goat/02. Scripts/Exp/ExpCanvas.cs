using System;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Exp
{
    public class ExpCanvas : MonoBehaviour
    {
        [SerializeField] private ExpManager expManager;
        [SerializeField] private ExpTextUI expTextUI;
        [SerializeField] private ExpSlider expSlider;
        
        private int _displayLevel;
        
        private void Awake()
        {
            expManager.OnLevelChanged += HandleLevelChange;
            expManager.OnExpChanged += HandleExpChange;
        }

        private void Start()
        {
            _displayLevel = expManager.CurrentLevel;
            
            expTextUI.LevelChange(_displayLevel);
            expSlider.SetFill(expManager.CurrentExp, expManager.ExpTableSo.GetRequiredExp(expManager.CurrentLevel));
        }

        private void OnDestroy()
        {
            expManager.OnLevelChanged -= HandleLevelChange;
            expManager.OnExpChanged -= HandleExpChange;
        }
        
        private void HandleExpChange(int levelUpCount,int currentExp, int maxExp)
        {
            expSlider.SetSmoothFill(levelUpCount,currentExp, maxExp, IncreaseLevelTextOneStep);
        }

        private void HandleLevelChange(int afterLevel, int beforeLevel)
        {
            _displayLevel = beforeLevel;

            expTextUI.LevelChange(_displayLevel);
        }
        
        private void IncreaseLevelTextOneStep()
        {
            _displayLevel++;
            expTextUI.LevelChange(_displayLevel);
        }
    }
}