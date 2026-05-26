using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AppQuitButton : Button
{
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        Application.Quit();
    }
}
