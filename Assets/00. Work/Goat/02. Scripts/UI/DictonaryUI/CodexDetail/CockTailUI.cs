using System;
using _00._Work.Lusaload._02._Scripts.SO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Goat._02._Scripts.UI.DictonaryUI.CodexDetail
{
    public class CockTailUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image cockTailImage;
        [SerializeField] private TextMeshProUGUI cockTailName;
        [SerializeField] private TextMeshProUGUI cokcTailDescription;
        [SerializeField] private Slider sourSlider;
        [SerializeField] private Slider sugarSlider;
        [SerializeField] private Slider bitterSlider;

        public event Action OnClickExitBtn;

        public void SetView(CocktailRecipeSO  slotSo)
        {
            cockTailImage.sprite = slotSo.cocktailIcon;
            cockTailName.text = slotSo.cocktailName;
            cokcTailDescription.text = slotSo.description;
            
            SetScore(slotSo.sourness, sourSlider);
            SetScore(slotSo.sweetness, sugarSlider);
            SetScore(slotSo.bitterness, bitterSlider);
        }

        private void SetScore(int num, Slider slider)
        {
            slider.value = num;
        }
        
        public void ClickExitBtn()
        {
            OnClickExitBtn?.Invoke();
        }
    }
}