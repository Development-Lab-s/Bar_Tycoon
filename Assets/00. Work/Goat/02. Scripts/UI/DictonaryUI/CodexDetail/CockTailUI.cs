using System;
using _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex.Data;
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

        public void SetView(CockTailSlotSo  slotSo)
        {
            cockTailImage.sprite = slotSo.CockTailImage;
            cockTailName.text = slotSo.CockTailName;
            cokcTailDescription.text = slotSo.CokcTailDescription;
            
            SetScore(slotSo.SourNum, sourSlider);
            SetScore(slotSo.SugarNum, sugarSlider);
            SetScore(slotSo.BitterNum, bitterSlider);
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