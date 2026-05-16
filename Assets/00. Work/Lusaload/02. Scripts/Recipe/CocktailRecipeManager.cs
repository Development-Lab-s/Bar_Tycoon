using System;
using _00._Work.Lusaload._02._Scripts.SO;
using _00._Work.Lusaload._02._Scripts.UI.CocktailShaker;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.Recipe
{
    // 칵테일 레시피와 제조 시퀀스를 관리하고, 씬 내 수신자에게 의존성을 주입하는 중앙 매니저
    public class CocktailRecipeManager : MonoBehaviour, IRecipeReader, IRecipeWriter, ISequenceReader
    {
        [field:SerializeField] public CocktailRecipeSO CurrentRecipe { get; private set; } // 현재 활성 칵테일 레시피
        public CocktailOrderSequence CurrentSequence { get; private set; }                  // 레시피에서 생성된 제조 시퀀스
        public event Action<CocktailOrderSequence> OnSequenceChanged;                       // 시퀀스가 교체될 때 발행

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

        // 씬의 모든 MonoBehaviour를 순회해 IRecipeReaderReceiver·IRecipeWriterReceiver·ISequenceReaderReceiver에 this를 주입
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

        

        // 새 레시피를 설정하고 그에 맞는 시퀀스를 생성
        public void SetRecipe(CocktailRecipeSO recipeSO)
        {
            CurrentRecipe = recipeSO;
            CreateSequenceFromRecipe(recipeSO);
        }

        // 레시피와 시퀀스를 초기화하고 null 시퀀스 변경 이벤트를 발행
        public void ClearRecipe()
        {
            CurrentRecipe = null;
            CurrentSequence = null;
            OnSequenceChanged?.Invoke(null);
        }
        
        // 레시피의 재료 목록으로 CocktailOrderSequence를 생성하고 OnSequenceChanged를 발행
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
