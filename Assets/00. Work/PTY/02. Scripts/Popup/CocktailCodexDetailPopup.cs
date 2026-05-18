using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _00._Work.Lusaload._02._Scripts.SO;

public class CocktailCodexDetailPopup : UIPopup
{
    [SerializeField] private TextMeshProUGUI cocktailNameText;
    [SerializeField] private Image cocktailIconImage;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI priceText;

    [SerializeField] private RatingBar sourness;    // 신맛
    [SerializeField] private RatingBar sweetness;   // 당도
    [SerializeField] private RatingBar bitterness;  // 쓴맛

    public void Open(CocktailRecipeSO data)
    {
        gameObject.SetActive(false);
        Bind(data);
        UIManager.Instance.PushPopup(gameObject);
    }

    private void Bind(CocktailRecipeSO data)
    {
        cocktailNameText.text  = data.cocktailName;
        cocktailIconImage.sprite = data.cocktailIcon;
        descriptionText.text   = data.description;
        //priceText.text         = $"{data.price} G";

        sourness.SetRating(data.sourness);
        sweetness.SetRating(data.sweetness);
        bitterness.SetRating(data.bitterness);
    }
}