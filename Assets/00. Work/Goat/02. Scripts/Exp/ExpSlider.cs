using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Goat._02._Scripts.Exp
{
    public class ExpSlider : MonoBehaviour
    {
        [SerializeField] private Image expSliderImage;

        public void SetFill(int amount, int max)
        {
            if (max <= 0)
            {
                expSliderImage.fillAmount = 1f;
                return;
            }

            expSliderImage.fillAmount = Mathf.Clamp01((float)amount / max);
        }
    }
}