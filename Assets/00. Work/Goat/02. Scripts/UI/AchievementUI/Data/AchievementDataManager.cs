using System;
using System.Collections.Generic;
using System.Linq;
using _00._Work.Goat._02._Scripts.Events;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.AchievementUI.Data
{
    [DefaultExecutionOrder(-10)]
    public class AchievementDataManager : MonoBehaviour
    {
        [field: SerializeField] public List<AchievementData> Achievements { get; private set; }
        [SerializeField] private EventChannelSO eventChannel;

        private Dictionary<AchievementType, AchievementData> _achievementDatasDict;
        
        public static AchievementDataManager Instance { get; private set; }
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            _achievementDatasDict = Achievements.ToDictionary(data => data.AchievementDataSO.AchievementType);
            eventChannel.AddListener<AchievementEvent>(HandleAchieveEvent);
        }

        private void OnDestroy()
        {
            Instance = null;
            eventChannel.RemoveListener<AchievementEvent>(HandleAchieveEvent);
        }
        
        private void HandleAchieveEvent(AchievementEvent obj)
        {
            if (_achievementDatasDict.TryGetValue(obj.achievementType, out AchievementData achievementData))
            {
                achievementData.AddDegree(obj.amount);
            }
        }
    }
}