using _00._Work.Lusaload._02._Scripts.SO;
using _00._Work.Lusaload._02._Scripts.UI.AlcoholList;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Lusaload._02._Scripts.UI.ItemSelectPanel
{
    public class ItemSelectPanelUI : MonoBehaviour
    {
        [SerializeField] private AlcoholListSO allIngredients;
        [SerializeField] private Transform contentParent;
        [SerializeField] private BaseAlcoholButtonUI buttonPrefab;

        [Header("Tab Buttons")]
        [SerializeField] private Button baseAlcoholTab;
        [SerializeField] private Button drinkTab;
        [SerializeField] private Button garnishTab;

        private IngredientCategory _currentCategory = IngredientCategory.BaseAlcohol;

        private void Start()
        {
            baseAlcoholTab.onClick.AddListener(() => ShowCategory(IngredientCategory.BaseAlcohol));
            drinkTab.onClick.AddListener(() => ShowCategory(IngredientCategory.Drink));
            garnishTab.onClick.AddListener(() => ShowCategory(IngredientCategory.Garnish));

            ShowCategory(_currentCategory);
        }

        public void ShowCategory(IngredientCategory category)
        {
            _currentCategory = category;
            UpdateTabState();
            RebuildList();
        }

        private void UpdateTabState()
        {
            baseAlcoholTab.interactable = _currentCategory != IngredientCategory.BaseAlcohol;
            drinkTab.interactable = _currentCategory != IngredientCategory.Drink;
            garnishTab.interactable = _currentCategory != IngredientCategory.Garnish;
        }

        private void RebuildList()
        {
            ClearContent();

            if (allIngredients == null) return;

            foreach (BaseAlcoholDataSO data in allIngredients.alcoholList)
            {
                if (data == null || data.category != _currentCategory) continue;
                BaseAlcoholButtonUI btn = Instantiate(buttonPrefab, contentParent);
                btn.SetData(data);
            }
        }

        private void ClearContent()
        {
            for (int i = contentParent.childCount - 1; i >= 0; i--)
                Destroy(contentParent.GetChild(i).gameObject);
        }
    }
}
