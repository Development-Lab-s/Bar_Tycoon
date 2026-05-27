using UnityEngine;
using UnityEngine.UI;

public class SliderFill : MonoBehaviour
{
    [SerializeField] private CharacterRegisterSO registerSO;
    [SerializeField] private CharListSO charList;
    [SerializeField] private Image _slider;

    private int _choseCharacter;

    public int ChoseCharacter
    {
        get => _choseCharacter;
        set
        {
            _choseCharacter = value;
            SliderAmount();
        }
    }

    private void OnEnable()
    {
        SliderAmount();
    }

    private void SliderAmount()
    {
        if (registerSO == null || charList == null) return;
        if (_choseCharacter < 0 || _choseCharacter >= charList.charList.Length) return;

        var charSO = charList.charList[_choseCharacter]?.charLevel;
        if (charSO == null) return;

        var ctrl = registerSO.GetById(charSO.id);
        _slider.fillAmount = ctrl != null ? ctrl.GetExpRatio() : 0f;
    }
}
