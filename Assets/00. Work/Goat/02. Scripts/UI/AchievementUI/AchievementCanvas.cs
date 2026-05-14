using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.UI.AchievementUI.Data;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.AchievementUI
{
    public class AchievementCanvas : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AchievementDataManager achievementDataManager;
        [SerializeField] private AchievementSlotContainer achievementSlotContainer;
        [SerializeField] private GameObject achievementObject;
        [SerializeField] private AchieveTopUI achieveTopUI;

        [SerializeField] private EventChannelSO coinChannelSo;

        private void OnEnable()
        {
            achieveTopUI.OnIsCompleteBtnClick += achievementSlotContainer.ShowContent;
            achievementSlotContainer.OnClickAchievementBtn += HandleClickAchievementBtn;
        }

        private void Start()
        {
            achievementSlotContainer.InitData(achievementDataManager.Achievements);
        }

        private void OnDisable()
        {
            achieveTopUI.OnIsCompleteBtnClick -= achievementSlotContainer.ShowContent;
            achievementSlotContainer.OnClickAchievementBtn -= HandleClickAchievementBtn;
        }
        
        private void HandleClickAchievementBtn(AchievementData data)
        {
            coinChannelSo.RaiseEvent(new CoinEvent().Init(data.AchievementDataSO.AchieveCoin));
            data.GetAwardTrue();
        }

        [ContextMenu("Show UI")]
        public void ShowUI() // ui보여줄때 이거 클릭
        {
            achievementObject.SetActive(true);
        }

        public void ExitBtn()
        {
            achievementObject.SetActive(false);
        }
    }
}