using UnityEngine;
using UnityEngine.UI;

public class SliderFill : MonoBehaviour
{
    [SerializeField]
    private PlayerCharController[] charController;

    private int _choseCharacter;

    public int ChoseCharacter
    {
        get
        {
            return _choseCharacter;
        }
        set
        {
            _choseCharacter = value;

            SliderAmount();
        }
    }
    [SerializeField]private Image _slider;
    private void OnEnable()
    {
        SliderAmount();
    }

    private void SliderAmount()
    {
        if (charController == null ||
            charController.Length == 0)
            return;

        if (_choseCharacter < 0 ||
            _choseCharacter >= charController.Length)
            return;

        _slider.fillAmount =
            charController[_choseCharacter]
            .GetExpRatio();
    }
}