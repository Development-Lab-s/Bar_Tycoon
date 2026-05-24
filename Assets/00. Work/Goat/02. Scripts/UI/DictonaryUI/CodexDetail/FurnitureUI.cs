using System;
using BBJ.GridSystem.Objects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Goat._02._Scripts.UI.DictonaryUI.CodexDetail
{
    public class FurnitureUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image cockTailImage;
        [SerializeField] private TextMeshProUGUI cockTailName;
        [SerializeField] private TextMeshProUGUI cokcTailDescription;
        
        public event Action OnClickExitBtn;
        
        public void SetView(ObjectDataSO  slotSo)
        {
            cockTailImage.sprite = slotSo.Icon;
            cockTailName.text = slotSo.DisplayName;
            cokcTailDescription.text = slotSo.Description;
        }
        
        public void ClickExitBtn()
        {
            OnClickExitBtn?.Invoke();
        }
    }
}