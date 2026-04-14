using _00._Work.Lusaload._02._Scripts.SO;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _00._Work.Lusaload._02._Scripts
{
    public class CocktailShaker : MonoBehaviour, IDropHandler
    {
        [SerializeField] private CocktailRecipeSO cocktailRecipe;
        
        public void OnDrop(PointerEventData eventData)
        {
            
        }

        private void AddAlcohol(BaseAlcoholDataSO alcohol)
        {
            
        }
    }
}