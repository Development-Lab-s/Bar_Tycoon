using System;
using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex;
using _00._Work.Goat._02._Scripts.UI.DictonaryUI.CodexDetail;
using _00._Work.Lusaload._02._Scripts.SO;
using BBJ.GridSystem.Objects;
using BBJ.WorkplaceSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.DictonaryUI
{
    public class CodexCanvas : MonoBehaviour
    {
        [Header("codex")]
        [SerializeField] private GameObject codex;
        [SerializeField] private CockTailContent cockTailContent;
        [SerializeField] private FurnitureContent furnitureContent;

        [Header("codexDetail")] 
        [SerializeField] private CockTailUI cockTailUI;
        [SerializeField] private FurnitureUI furnitureUI;
        
        [field: SerializeField] public CocktailRecipeDatabaseSO UnlockedCockTails { get; private set; }
        [field : SerializeField] public ObjectDataBase UnlockedObjectData { get; private set; }

        private void Awake()
        {
            cockTailContent.OnClickBtn += HandleOnClickBtnCockTail;
            furnitureContent.OnClickBtn += HandleOnClickBtnFurniture;
            cockTailUI.OnClickExitBtn += HandleOnExitClickBtn;
            furnitureUI.OnClickExitBtn += HandleOnExitClickBtn;
        }
        
        private void OnDestroy()
        {
            cockTailContent.OnClickBtn -= HandleOnClickBtnCockTail;
            furnitureContent.OnClickBtn -= HandleOnClickBtnFurniture;
            cockTailUI.OnClickExitBtn -= HandleOnExitClickBtn;
            furnitureUI.OnClickExitBtn -= HandleOnExitClickBtn;
        }

        [ContextMenu("Show UI")]
        public void OpenCodex() // 도감 활성화 할때 이거 쓰셈
        {
            codex.SetActive(true);
            cockTailUI.gameObject.SetActive(false);
            furnitureUI.gameObject.SetActive(false);

            cockTailContent.SetView(UnlockedCockTails.recipes);
            furnitureContent.SetView(UnlockedObjectData.Recipes);
        }

        private void HandleOnClickBtnCockTail(CocktailRecipeSO obj)
        {
            codex.SetActive(false);
            cockTailUI.gameObject.SetActive(true);
            cockTailUI.SetView(obj);
        }
        
        private void HandleOnClickBtnFurniture(ObjectDataSO obj)
        {
            codex.SetActive(false);
            furnitureUI.gameObject.SetActive(true);
            furnitureUI.SetView(obj);
        }
        
        private void HandleOnExitClickBtn()
        {
            codex.SetActive(true);
            cockTailUI.gameObject.SetActive(false);
            furnitureUI.gameObject.SetActive(false);
        }
        
        public void ExitBtnClick()
        {
            codex.SetActive(false);
        }
    }
}