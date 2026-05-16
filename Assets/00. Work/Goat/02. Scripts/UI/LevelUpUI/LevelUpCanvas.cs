using System;
using _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex.Data;
using _00._Work.Lusaload._02._Scripts.SO;
using TMPro;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI
{
    public class LevelUpCanvas : MonoBehaviour
    {
        [Header("SO")]
        [SerializeField] private LevelUpRewardSOs levelUpRewardSOs;
        
        [Header("Reference")]
        [SerializeField] private LevelUpRewardManager levelUpRewardManager;
        [SerializeField] private LevelUpContainer levelUpContainer;
        [SerializeField] private GameObject levelUpObject;
        [SerializeField] private TextMeshProUGUI levelText;

        private void Awake()
        {
            levelUpRewardManager.OnCockTailAdd += HandleCockTailAdd;
        }

        private void OnDestroy()
        {
            levelUpRewardManager.OnCockTailAdd -= HandleCockTailAdd;
        }

        private void HandleCockTailAdd(int level, CocktailRecipeSO cockTailSo)
        {
            ShowUI();
            levelText.text = level.ToString();
            levelUpContainer.SpawnSlot(cockTailSo);
        }

        public void ExitBtn()
        {
            levelUpObject.SetActive(false);
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