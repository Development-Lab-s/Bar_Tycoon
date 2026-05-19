using UnityEngine;
using _00._Work.Lusaload._02._Scripts.SO;

public class CocktailCodexItem : MonoBehaviour
{ 
    public CocktailRecipeSO data;
    [SerializeField] private CocktailCodexDetailPopup detailPopup;

    public void OnClick()
    {
        detailPopup.Open(data);
    }
}