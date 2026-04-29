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
        [SerializeField] private Image panelImage;
        [SerializeField] private Slider slider;
        [SerializeField] private Button achievementButton;
        
        public AchievementData MyData { get; private set; }
        public void SetData(AchievementData data)
        {
            MyData = data;
            
            MyData.OnDegreeChange += Refresh;
            
            titleText.text = data.AchievementDataSO.AchievementName;
            descriptionText.text = data.AchievementDataSO.AchievementDescription;
            achievementSliderText.text = $"{data.NowAchievementDegree} / {data.AchievementDataSO.TargetAchievementDegree}";
            slider.value = (float)data.NowAchievementDegree / data.AchievementDataSO.TargetAchievementDegree;
        }

        private void ClickAchievementBtn()
        {
            if (MyData.GetAward) return;
            
            //보상받는시스템 만들기
            MyData.GetAwardTrue();
        }
        
        private void Refresh()
        {
            achievementSliderText.text = $"{MyData.NowAchievementDegree} / {MyData.AchievementDataSO.TargetAchievementDegree}";
            slider.value = (float)MyData.NowAchievementDegree / MyData.AchievementDataSO.TargetAchievementDegree;
        }
    }
}