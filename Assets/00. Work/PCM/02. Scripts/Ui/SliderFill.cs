using Gamelib.EventSystem;
using UnityEngine;
using UnityEngine.UI;

public class SliderFill : MonoBehaviour
{
    [SerializeField]private PlayerCharController charController;
    private Image Slider;
    private void Awake()
    {
        Slider = GetComponent<Image>();
    }
    private void OnEnable()
    {
       Slider.fillAmount = charController.GetExpRatio();
    }
}
