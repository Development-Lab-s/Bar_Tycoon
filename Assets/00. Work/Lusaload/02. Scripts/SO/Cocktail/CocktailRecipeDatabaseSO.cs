using System;
using System.Collections.Generic;
using System.Linq;
using _00._Work.Goat._02._Scripts.SaveCode;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    // 게임에 등록된 모든 칵테일 레시피 SO를 한 곳에서 관리하는 데이터베이스 SO
    [CreateAssetMenu(fileName = "CocktailDataBase", menuName = "Alcohol/CocktailDataBaseSO", order = 0)]
    public class CocktailRecipeDatabaseSO : ScriptableObject, ISerializationCallbackReceiver
    {
        [Header("Save")]
        [SerializeField] private SaveFileNameSO saveFileNameSO;
        
        [Header("All Cocktail Recipes")]
        [SerializeField] private List<CocktailRecipeSO> allRecipes = new();
        
        [SerializeField] private List<CocktailRecipeSO> itemListForSerialize = new();

        [Header("first")] 
        [SerializeField] private CocktailRecipeSO firstItem;

        public HashSet<CocktailRecipeSO> recipes = new();
        

        public IReadOnlyCollection<CocktailRecipeSO> Recipes => recipes;
        
        
        private JsonSaveService _saveService;
        
        private void OnEnable()
        {
            if (saveFileNameSO != null)
                _saveService = new JsonSaveService(saveFileNameSO);

            LoadSerializedListToHashSet();

            // 게임 시작 시 저장된 데이터 불러오기
            Load();
        }

        public void AddCockTail(CocktailRecipeSO cocktailRecipeSo)
        {
            if (cocktailRecipeSo == null)
                return;

            recipes.Add(cocktailRecipeSo);
            Save();
        }
        
        public void Load()
        {
            if (_saveService == null)
            {
                if (saveFileNameSO == null)
                    return;

                _saveService = new JsonSaveService(saveFileNameSO);
            }

            CocktailRecipeDatabaseSaveData saveData =
                _saveService.Load<CocktailRecipeDatabaseSaveData>();

            // 저장 데이터가 없으면 처음 칵테일 지급
            if (saveData == null)
            {
                recipes.Clear();

                if (firstItem != null)
                    recipes.Add(firstItem);

                Save();
                return;
            }

            recipes.Clear();

            foreach (string recipeName in saveData.cocktailRecipeNames)
            {
                CocktailRecipeSO recipe = allRecipes
                    .FirstOrDefault(x => x != null && x.name == recipeName);

                if (recipe != null)
                    recipes.Add(recipe);
            }
            
            // 저장 파일은 있는데 내용이 비어있으면 firstItem 지급
            if (recipes.Count == 0 && firstItem != null)
            {
                recipes.Add(firstItem);
                Save();
            }
        }
        
        public void Save()
        {
            if (_saveService == null)
            {
                if (saveFileNameSO == null)
                {
                    Debug.LogWarning("SaveFileNameSO가 없습니다.");
                    return;
                }

                _saveService = new JsonSaveService(saveFileNameSO);
            }

            CocktailRecipeDatabaseSaveData saveData = new CocktailRecipeDatabaseSaveData();

            foreach (var recipe in recipes)
            {
                if (recipe == null)
                    continue;

                saveData.cocktailRecipeNames.Add(recipe.name);
            }

            _saveService.Save(saveData);
        }

        public void OnBeforeSerialize()
        {
            itemListForSerialize.Clear();

            foreach (var item in recipes)
            {
                if (item != null) 
                    itemListForSerialize.Add(item);
            }
        }

        public void OnAfterDeserialize()
        {
            LoadSerializedListToHashSet();
        }
        
        private void LoadSerializedListToHashSet()
        {
            if (recipes == null)
                recipes = new HashSet<CocktailRecipeSO>();

            recipes.Clear();

            foreach (var item in itemListForSerialize)
            {
                if (item != null)
                    recipes.Add(item);
            }
        }

        public void Reset()
        {
            recipes.Clear();
            itemListForSerialize.Clear();
        }
    }
}