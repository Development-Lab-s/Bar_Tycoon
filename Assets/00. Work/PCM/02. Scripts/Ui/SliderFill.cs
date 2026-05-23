using Gamelib.EventSystem;
using UnityEngine;
using UnityEngine.UI;

public class SliderFill : MonoBehaviour
{
    [SerializeField]private PlayerCharController[] charController;
    [SerializeField]private int choseCharacter; 
    private Image Slider;
    private void Awake()
    {
        Slider = GetComponent<Image>();
    }
    private void OnEnable()
    {
        SliderAmount();
    }

    private void SliderAmount()
    {
        Slider.fillAmount = charController[choseCharacter].GetExpRatio();
    }
}
