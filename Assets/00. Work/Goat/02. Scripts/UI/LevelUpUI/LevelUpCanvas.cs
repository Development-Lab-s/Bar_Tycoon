using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Lusaload._02._Scripts.SO;
using Gamelib.EventSystem;
using TMPro;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI
{
    public class LevelUpCanvas : MonoBehaviour
    {
        [Header("RewardSO")]
        [SerializeField] private LevelUpRewardSOs levelUpRewardSOs;

        [Header("eventChannel")] 
        [SerializeField] private EventChannelSO levelUpRewardeExitBtnClickEvent;
        
        [Header("Reference")]
        [SerializeField] private LevelUpRewardManager levelUpRewardManager;
        [SerializeField] private LevelUpContainer levelUpCocktailContainer;
        [SerializeField] private LevelUpContainer levelUpFunctionContainer;
        [SerializeField] private GameObject levelUpObject;
        [SerializeField] private TextMeshProUGUI levelText;

        private void Awake()
        {
            levelUpRewardManager.OnCockTailAdd += HandleCockTailAdd;
            levelUpRewardManager.OnFuncAdd += HandleFuncAdd;
        }

        private void OnDestroy()
        {
            levelUpRewardManager.OnCockTailAdd -= HandleCockTailAdd;
            levelUpRewardManager.OnFuncAdd -= HandleFuncAdd;
        }

        private void HandleFuncAdd(Sprite sprite, string description, int level)
        {
            ShowUI();
            levelText.text = level.ToString();
            levelUpFunctionContainer.SpawnSlotSprite(sprite, description);
        }
        private void HandleCockTailAdd(int level, CocktailRecipeSO cockTailSo)
        {
            ShowUI();
            levelText.text = level.ToString();
            levelUpCocktailContainer.SpawnSlotCockTail(cockTailSo);
        }

        public void ExitBtn()
        {
            levelUpObject.SetActive(false);
            levelUpRewardeExitBtnClickEvent.RaiseEvent(new LevelUpRewardeExitBtnClickEvent().Init(true));
        }

        [ContextMenu("Show UI")]
        public void ShowUI()
        {
            if (levelUpObject.activeSelf)
                return;
            
            levelUpObject.SetActive(true);
        }
    }
}