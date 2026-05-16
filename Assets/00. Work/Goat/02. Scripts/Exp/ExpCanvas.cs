using System;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Exp
{
    public class ExpCanvas : MonoBehaviour
    {
        [SerializeField] private ExpManager expManager;
        [SerializeField] private ExpTextUI expTextUI;
        [SerializeField] private ExpSlider expSlider;
        private void Awake()
        {
            expManager.OnLevelChanged += HandleLevelChange;
            expManager.OnExpChanged += HandleExpChange;
        }

        private void Start()
        {
            expTextUI.LevelChange(expManager.CurrentLevel);
            expSlider.SetFill(expManager.CurrentExp, expManager.ExpTableSo.GetRequiredExp(expManager.CurrentLevel));
        }

        private void OnDestroy()
        {
            expManager.OnLevelChanged -= HandleLevelChange;
            expManager.OnExpChanged -= HandleExpChange;
        }
        
        private void HandleExpChange(int levelUpCount,int currentExp, int maxExp)
        {
            expSlider.SetSmoothFill(levelUpCount,currentExp, maxExp);
        }

        private void HandleLevelChange(int level, int _)
        {
            expTextUI.LevelChange(level);
        }
    }
}