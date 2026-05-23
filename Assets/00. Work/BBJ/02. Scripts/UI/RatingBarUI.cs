using UnityEngine;
using UnityEngine.UI;

namespace BBJ.UI
{
    public class RatingBarUI : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private Color _fillColor;

        private void Awake()
        {
            _fillImage.color = _fillColor;
        }

        public void SetRating(int value)
        {
            _fillImage.fillAmount = Mathf.Clamp01(value / 5f);
        }
    }
}
