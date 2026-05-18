using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

public class MenuOpen : MonoBehaviour
{
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private RectTransform menuPopUp;
    
    public void OpenMenu()
    {
        if (UIManager.Instance.CurPopup == null)
        {
            menuPopUp.gameObject.SetActive(true);
            var x = menuPopUp.anchoredPosition.x;
            menuPopUp.anchoredPosition = new Vector2(x, -Screen.height);
            
            LMotion.Create(
                    new Vector2(x, -Screen.height),
                    new Vector2(x, 0f),
                    duration)
                .WithEase(Ease.OutCubic)
                .BindToAnchoredPosition(menuPopUp);
        }
    }

    public async void CloseMenu()
    {
        var tween = LMotion.Create(
                new Vector2(menuPopUp.anchoredPosition.x, 0f),
                new Vector2(menuPopUp.anchoredPosition.x, -Screen.height * 2),
                duration)
            .WithEase(Ease.OutCubic)
            .BindToAnchoredPosition(menuPopUp);

        await tween;
        
        menuPopUp.gameObject.SetActive(false);
    }
}
