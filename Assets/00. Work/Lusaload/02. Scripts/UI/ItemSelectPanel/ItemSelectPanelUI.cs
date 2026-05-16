using _00._Work.Lusaload._02._Scripts.SO;
using _00._Work.Lusaload._02._Scripts.UI.AlcoholList;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Lusaload._02._Scripts.UI.ItemSelectPanel
{
    // 카테고리 탭 전환과 아이템 목록 표시를 담당하는 패널 UI
    public class ItemSelectPanelUI : MonoBehaviour
    {
        [SerializeField] private IngredientDatabaseSO ingredientDatabase;
        [SerializeField] private Transform contentParent;
        [SerializeField] private BaseAlcoholButtonUI buttonPrefab;

        [Header("Tab Buttons")]
        [SerializeField] private Button baseAlcoholTab;
        [SerializeField] private Button drinkTab;
        [SerializeField] private Button garnishTab;

        [Header("Tab Visuals")]
        [SerializeField] private CategoryTabButton baseAlcoholTabVisual;
        [SerializeField] private CategoryTabButton drinkTabVisual;
        [SerializeField] private CategoryTabButton garnishTabVisual;

        private IngredientCategory _currentCategory = IngredientCategory.BaseAlcohol;

        private void Start()
        {
            baseAlcoholTab.onClick.AddListener(() => ShowCategory(IngredientCategory.BaseAlcohol));
            drinkTab.onClick.AddListener(() => ShowCategory(IngredientCategory.Drink));
            garnishTab.onClick.AddListener(() => ShowCategory(IngredientCategory.Garnish));

            ShowCategory(_currentCategory);
        }

        // 카테고리 변경 후 탭 상태와 목록을 갱신
        public void ShowCategory(IngredientCategory category)
        {
            _currentCategory = category;
            UpdateTabState();
            RebuildList();
        }

        // 현재 카테고리에 맞게 버튼 비활성화 및 비주얼 선택 상태 동기화
        private void UpdateTabState()
        {
            baseAlcoholTab.interactable = _currentCategory != IngredientCategory.BaseAlcohol;
            drinkTab.interactable = _currentCategory != IngredientCategory.Drink;
            garnishTab.interactable = _currentCategory != IngredientCategory.Garnish;

            baseAlcoholTabVisual?.SetSelected(_currentCategory == IngredientCategory.BaseAlcohol);
            drinkTabVisual?.SetSelected(_currentCategory == IngredientCategory.Drink);
            garnishTabVisual?.SetSelected(_currentCategory == IngredientCategory.Garnish);
        }

        // 현재 카테고리의 재료 버튼을 content에 다시 생성
        private void RebuildList()
        {
            ClearContent();
            if (ingredientDatabase == null) return;

            foreach (BaseAlcoholDataSO data in ingredientDatabase.GetList(_currentCategory))
            {
                if (data == null) continue;
                BaseAlcoholButtonUI btn = Instantiate(buttonPrefab, contentParent);
                btn.SetData(data);
            }
        }

        // content 하위의 모든 버튼 오브젝트 제거
        private void ClearContent()
        {
            for (int i = contentParent.childCount - 1; i >= 0; i--)
                Destroy(contentParent.GetChild(i).gameObject);
        }
    }
}
