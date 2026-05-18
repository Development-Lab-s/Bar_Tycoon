using System;
using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex;
using _00._Work.Goat._02._Scripts.UI.DictonaryUI.CodexDetail;
using _00._Work.Lusaload._02._Scripts.SO;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.DictonaryUI
{
    public class CodexCanvas : MonoBehaviour
    {
        [Header("codex")]
        [SerializeField] private GameObject codex;
        [SerializeField] private CockTailContent cockTailContent;

        [Header("codexDetail")] 
        [SerializeField] private CockTailUI cockTailUI;
        
        [field: SerializeField] public CocktailRecipeDatabaseSO UnlockedCockTails { get; private set; }

        private void Awake()
        {
            cockTailContent.OnClickBtn += HandleOnClickBtn;
            cockTailUI.OnClickExitBtn += HandleOnExitClickBtn;
        }
        
        private void OnDestroy()
        {
            cockTailContent.OnClickBtn -= HandleOnClickBtn;
            cockTailUI.OnClickExitBtn -= HandleOnExitClickBtn;
        }
        
        [ContextMenu("Show UI")]
        public void OpenCodex() // 도감 활성화 할때 이거 쓰셈
        {
            codex.SetActive(true);
            cockTailUI.gameObject.SetActive(false);

            cockTailContent.SetView(UnlockedCockTails.recipes);
        }

        private void HandleOnClickBtn(CocktailRecipeSO obj)
        {
            codex.SetActive(false);
            cockTailUI.gameObject.SetActive(true);
            cockTailUI.SetView(obj);
        }
        
        private void HandleOnExitClickBtn()
        {
            codex.SetActive(true);
            cockTailUI.gameObject.SetActive(false);
        }
        
        public void ExitBtnClick()
        {
            codex.SetActive(false);
        }
    }
}