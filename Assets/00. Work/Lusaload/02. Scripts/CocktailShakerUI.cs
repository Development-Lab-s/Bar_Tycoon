using _00._Work.Lusaload._02._Scripts.SO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _00._Work.Lusaload._02._Scripts
{
    public class CocktailShakerUI : MonoBehaviour, IDropHandler
    {
        [SerializeField] private CocktailRecipeDatabaseSO recipeDatabase;

        [SerializeField] private TextMeshProUGUI currentRecipeText;
        
        
        public void OnDrop(PointerEventData eventData)
        {
            
        }
    }
}