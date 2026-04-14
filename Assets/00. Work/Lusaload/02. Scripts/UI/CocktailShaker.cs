using System.Collections.Generic;
using _00._Work.Lusaload._02._Scripts.SO;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _00._Work.Lusaload._02._Scripts.UI
{
    public class CocktailShaker : MonoBehaviour, IDropHandler
    {
        [SerializeField] private CocktailRecipeSO cocktailRecipe;
        private List<BaseAlcoholDataSO> _currentAlcoholList;
        
        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            // 드래그해서 올린 버튼의 정보를 가져옵니다
            BaseAlcoholButtonUI alcoholItemUI = eventData.pointerDrag.GetComponent<BaseAlcoholButtonUI>();
            DraggableItem draggableItem = eventData.pointerDrag.GetComponent<DraggableItem>();

            AddAlcohol(alcoholItemUI.Data);
            
            if (draggableItem != null)
                draggableItem.ReturnToOriginalParent();
        }

        private void AddAlcohol(BaseAlcoholDataSO alcohol)
        {
            if (cocktailRecipe == null)
            {
                Debug.LogWarning("셰이커에 CocktailRecipeSO가 연결되지 않았습니다.");
                return;
            }

            if (cocktailRecipe.cocktailRecipeList == null || cocktailRecipe.cocktailRecipeList.Count == 0)
            {
                Debug.LogWarning("레시피 안에 재료가 비어 있습니다.");
                return;
            }

            if (_currentAlcoholList.Contains(alcohol))
            {
                Debug.Log($"{alcohol.alcoholName} 은(는) 이미 셰이커에 들어가 있습니다.");
                return;
            }
            
            if (!cocktailRecipe.cocktailRecipeList.Contains(alcohol))
            {
                Debug.Log($"{alcohol.alcoholName} 은(는) {cocktailRecipe.cocktailName} 레시피에 없는 재료입니다.");
                return;
            }
            
            _currentAlcoholList.Add(alcohol);
            Debug.Log($"{alcohol.alcoholName} 추가됨");

            // 셰이커 안에 레시피 내, 모든 술이 들어 있는지 확인
            if (IsRecipeCompleted())
            {
                Debug.Log($"{cocktailRecipe.cocktailName}이 완성 되었습니다!");
            }
        }

        private bool IsRecipeCompleted()
        {
            if (cocktailRecipe == null || cocktailRecipe.cocktailRecipeList == null)
                return false;

            if (_currentAlcoholList.Count != cocktailRecipe.cocktailRecipeList.Count)
                return false;
            
            // 칵테일 레시피의 리스트를 전부 순회하여 모든 요소가 셰이커 안에 존재할 경우를 파악합니다.
            foreach (BaseAlcoholDataSO requiredAlcohol in cocktailRecipe.cocktailRecipeList)
            {
                if (requiredAlcohol == null)
                    continue;

                if (!_currentAlcoholList.Contains(requiredAlcohol))
                    return false;
            }

            return true;
        }
    }
}