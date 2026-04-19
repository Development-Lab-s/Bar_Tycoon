using System;
using _00._Work.Lusaload._02._Scripts.SO;
using _00._Work.Lusaload._02._Scripts.UI.CocktailShaker;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.Recipe
{
    public class CocktailRecipeManager : MonoBehaviour, IRecipeReader, IRecipeWriter, ISequenceReader
    {
        [field:SerializeField] public CocktailRecipeSO CurrentRecipe { get; private set; }
        public CocktailOrderSequence CurrentSequence { get; private set; }
        public event Action<CocktailOrderSequence> OnSequenceChanged;

        private void Awake()
        {
            Debug.Log($"[Manager] Awake 시작. CurrentRecipe = {CurrentRecipe}");
    
            InjectDependencies();
            Debug.Log($"[Manager] InjectDependencies 완료");

            if (CurrentRecipe != null)
            {
                Debug.Log($"[Manager] 레시피 있음: {CurrentRecipe.name}");
                Debug.Log($"[Manager] cocktailRecipeList null? {CurrentRecipe.cocktailRecipeList == null}");
                if (CurrentRecipe.cocktailRecipeList != null)
                    Debug.Log($"[Manager] 재료 수: {CurrentRecipe.cocktailRecipeList.Count}");
        
                CreateSequenceFromRecipe(CurrentRecipe);
                Debug.Log($"[Manager] CurrentSequence 생성됨? {CurrentSequence != null}");
            }
            else
            {
                Debug.LogError("[Manager] CurrentRecipe가 null입니다! 인스펙터에서 연결하세요.");
            }
        }

        private void InjectDependencies()
        {
            MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
 
            foreach (MonoBehaviour m in all)
            {
                if (m is IRecipeReaderReceiver readerReceiver)
                    readerReceiver.SetRecipeReader(this);
 
                if (m is IRecipeWriterReceiver writerReceiver)
                    writerReceiver.SetRecipeWriter(this);
 
                if (m is ISequenceReaderReceiver sequenceReceiver)
                    sequenceReceiver.SetSequenceReader(this);
            }
        }

        

        public void SetRecipe(CocktailRecipeSO recipeSO)
        {
            CurrentRecipe = recipeSO;
            CreateSequenceFromRecipe(recipeSO);
        }

        public void ClearRecipe()
        {
            CurrentRecipe = null;
            CurrentSequence = null;
            OnSequenceChanged?.Invoke(null);
        }
        
        private void CreateSequenceFromRecipe(CocktailRecipeSO recipe)
        {
            if (recipe == null || recipe.cocktailRecipeList == null || recipe.cocktailRecipeList.Count == 0)
            {
                CurrentSequence = null;
            }
            else
            {
                CurrentSequence = new CocktailOrderSequence(recipe.cocktailRecipeList);
            }
 
            OnSequenceChanged?.Invoke(CurrentSequence);
        }

    }
}
