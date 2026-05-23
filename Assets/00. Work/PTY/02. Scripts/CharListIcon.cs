using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharListIcon : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;

    public void Setup(CharacterDataSO data)
    {
        if (iconImage != null) iconImage.sprite = data.characterImage;
        if (nameText != null) nameText.text = data.characterName;
    }
}