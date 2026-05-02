using System;
using _00._Work.Goat._02._Scripts.UI.AchievementUI.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Goat._02._Scripts.UI.AchievementUI
{
    public class AchievementSlot : MonoBehaviour
    {
        [Header("Uis")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI achievementSliderText;
        [SerializeField] private TextMeshProUGUI coinTitle;
        [SerializeField] private Image panelImage;
        [SerializeField] private Slider slider;
        [SerializeField] private Button achievementButton;

        public event Action<AchievementData> OnClickAchievementBtn;

        public AchievementData MyData { get; private set; }

        private void OnEnable()
        {
            achievementButton.onClick.AddListener(ClickAchievementBtn);
        }

        private void OnDisable()
        {
            achievementButton.onClick.RemoveListener(ClickAchievementBtn);
        }
        public void SetData(AchievementData data)
        {
            if (MyData != null)
                MyData.OnChanged -= Refresh;
            
            MyData = data;
            MyData.OnChanged += Refresh;
            
            titleText.text = data.AchievementDataSO.AchievementName;
            descriptionText.text = data.AchievementDataSO.AchievementDescription;
            coinTitle.text = data.AchievementDataSO.AchieveCoin.ToString();
            Refresh();
        }
        
        private void OnDestroy()
        {
            if (MyData != null)
            {
                MyData.OnChanged -= Refresh;
            }
        }

        private void ClickAchievementBtn()
        {
            if (MyData.GetAward || !MyData.IsComplete) return;
            
            OnClickAchievementBtn?.Invoke(MyData);
        }
        
        private void Refresh()
        {
            achievementSliderText.text = $"{MyData.NowAchievementDegree} / {MyData.AchievementDataSO.TargetAchievementDegree}";
            slider.value = (float)MyData.NowAchievementDegree / MyData.AchievementDataSO.TargetAchievementDegree;
            panelImage.gameObject.SetActive(MyData.GetAward);
        }
    }
}