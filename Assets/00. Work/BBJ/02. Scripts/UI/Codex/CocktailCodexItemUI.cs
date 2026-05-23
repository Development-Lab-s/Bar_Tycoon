using UnityEngine;
using UnityEngine.EventSystems;
using _00._Work.Lusaload._02._Scripts.SO;

namespace BBJ.UI.Codex
{
    public class CocktailCodexItemUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CocktailRecipeSO           _data;
        [SerializeField] private CocktailCodexDetailPopupUI _detailPopup;

        public void OnPointerClick(PointerEventData eventData)
        {
            _detailPopup.Open(_data);
        }
    }
}
