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
        [SerializeField] private TextMeshProUGUI sourText;
        [SerializeField] private TextMeshProUGUI sugarText;
        [SerializeField] private TextMeshProUGUI bitterText;

        public event Action OnClickExitBtn;

        public void SetView(CockTailSlotSo  slotSo)
        {
            cockTailImage.sprite = slotSo.CockTailImage;
            cockTailName.text = slotSo.CockTailName;
            cokcTailDescription.text = slotSo.CokcTailDescription;
            
            SetScore(slotSo.SourNum, sourText);
            SetScore(slotSo.SugarNum, sugarText);
            SetScore(slotSo.BitterNum, bitterText);
        }

        private void SetScore(int num, TextMeshProUGUI text)
        {
            text.text = "";
            for (int i = 0; i < num; i++)
            {
                text.text += "●";
            }
        }
        
        public void ClickExitBtn()
        {
            OnClickExitBtn?.Invoke();
        }
    }
}