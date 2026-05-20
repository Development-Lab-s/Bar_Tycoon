using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ArbeitCategoryButton : MonoBehaviour
{
    [SerializeField] private ArbeitUpgradeUI upgradeUI;
    [SerializeField] private ArbeitEnum category;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite normalSprite;

    private RectTransform _rectTransform;
    private Image _image;

    public ArbeitEnum Category => category;
    public RectTransform RectTransform => _rectTransform;
    public Image Image => _image;
    public Sprite SelectedSprite => selectedSprite;
    public Sprite NormalSprite => normalSprite;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        _image = GetComponent<Image>();
    }

    public void OnClick()
    {
        upgradeUI.OnClickCategory(this);
    }

    public void SetSelected(bool selected)
    {
        _image.sprite = selected ? selectedSprite : normalSprite;
    }
}