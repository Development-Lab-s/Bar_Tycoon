using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.CheolYee
{
    public class ToggleBtnImage : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private Sprite startSprite;
        [SerializeField] private Sprite changeSprite;

        private bool _toggle;

        public void Toggle()
        {
            _toggle = !_toggle;
            targetImage.sprite = _toggle ? startSprite : changeSprite;
        }
    }
}