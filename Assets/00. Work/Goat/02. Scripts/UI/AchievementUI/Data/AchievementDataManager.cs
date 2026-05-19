using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.SaveCode;
using _00._Work.Goat._02._Scripts.UI.AchievementUI.Save;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.AchievementUI.Data
{
    [DefaultExecutionOrder(-10)]
    public class AchievementDataManager : MonoBehaviour
    {
        [field: SerializeField] public List<AchievementData> Achievements { get; private set; }
        [field: SerializeField] public SaveFileNameSO SaveFileName { get; private set; }
        [SerializeField] private EventChannelSO eventChannel;
        
        private Dictionary<AchievementType, AchievementData> _achievementDatasDict;
        private JsonSaveService  _jsonSaveService;
        
        private void Awake()
        {
            //저장관련
            _jsonSaveService = new JsonSaveService(SaveFileName);
            InitSaveData();
            
            _achievementDatasDict = Achievements.ToDictionary(data => data.AchievementDataSO.AchievementType);
            
            LoadSaveData();
            
            eventChannel.AddListener<AchievementEvent>(HandleAchieveEvent);
        }
        
        private void OnDestroy()
        {
            if (eventChannel != null)
                eventChannel.RemoveListener<AchievementEvent>(HandleAchieveEvent);

            foreach (AchievementData data in Achievements)
            {
                data.OnChanged -= HandleChanged;
            }
        }

        private void LoadSaveData()
        {
            if (!File.Exists(SaveFileName.SavePath))
            {
                Debug.Log("now file not exist");
                return;
            }
            
            AchieveSaveDataList jsonData = _jsonSaveService.Load<AchieveSaveDataList>();

            if (jsonData == null || jsonData.achieveSaveDatas == null)
            {
                Debug.LogWarning("Save data is empty or invalid.");
                return;
            }
            
            foreach (AchieveSaveData achieveDataSave in jsonData.achieveSaveDatas)
            {
                if (_achievementDatasDict.TryGetValue(achieveDataSave.achievementType,
                        out AchievementData achievementData))
                {
                    achievementData.ChangeAchieveData(achieveDataSave);
                }
            }
        }

        private void InitSaveData()
        {
            foreach (AchievementData data in Achievements)
            {
                data.AchieveSaveData.achievementType = data.AchievementDataSO.AchievementType;
                data.OnChanged += HandleChanged;
            }
        }

        private void HandleChanged(AchievementData data)
        {
            AchieveSaveDataList achieveDatas = new();

            foreach (AchievementData achieveDataSave in Achievements)
            {
                AchieveSaveData achieveSaveData = achieveDataSave.AchieveSaveData;
                achieveDatas.achieveSaveDatas.Add(achieveSaveData);
            }
            
            _jsonSaveService.Save(achieveDatas);
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