using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _00._Work.Lusaload._02._Scripts.SO;

namespace BBJ.UI.Codex
{
    public class CocktailCodexDetailPopupUI : UIPopup
    {
        [SerializeField] private TMP_Text  _cocktailName;
        [SerializeField] private Image     _cocktailIcon;
        [SerializeField] private TMP_Text  _description;
        [SerializeField] private RatingBarUI _sourness;
        [SerializeField] private RatingBarUI _sweetness;
        [SerializeField] private RatingBarUI _bitterness;

        public void Open(CocktailRecipeSO data)
        {
            Bind(data);
            UIManager.Instance.PushPopup(gameObject);
        }

        private void Bind(CocktailRecipeSO data)
        {
            _cocktailName.text   = data.cocktailName;
            _cocktailIcon.sprite = data.cocktailIcon;
            _description.text    = data.description;
            _sourness.SetRating(data.sourness);
            _sweetness.SetRating(data.sweetness);
            _bitterness.SetRating(data.bitterness);
        }
    }
}
