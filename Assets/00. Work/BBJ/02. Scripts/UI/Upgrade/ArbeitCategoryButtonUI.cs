using UnityEngine;
using UnityEngine.UI;

namespace BBJ.UI.Upgrade
{
    public class ArbeitCategoryButtonUI : MonoBehaviour
    {
        [SerializeField] private ArbeitUpgradeUI _upgradeUI;
        [SerializeField] private ArbeitEnum      _category;
        [SerializeField] private Sprite          _selectedSprite;
        [SerializeField] private Sprite          _normalSprite;

        private RectTransform _rect;
        private Image         _image;

        public ArbeitEnum    Category => _category;
        public RectTransform Rect     => _rect;

        private void Awake()
        {
            _rect  = transform as RectTransform;
            _image = GetComponent<Image>();
        }

        public void OnClick() => _upgradeUI.OnClickCategory(this);

        public void SetSelected(bool selected)
        {
            _image.sprite = selected ? _selectedSprite : _normalSprite;
        }
    }
}
