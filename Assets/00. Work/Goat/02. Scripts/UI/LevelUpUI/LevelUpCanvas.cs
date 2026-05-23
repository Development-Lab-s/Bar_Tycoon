using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Lusaload._02._Scripts.SO;
using Gamelib.EventSystem;
using TMPro;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI
{
    public class LevelUpCanvas : MonoBehaviour
    {
        [Header("RewardSO")]
        [SerializeField] private LevelUpRewardSOs levelUpRewardSOs;

        [Header("eventChannel")] 
        [SerializeField] private EventChannelSO cameraManagerSO;
        
        [Header("Reference")]
        [SerializeField] private LevelUpRewardManager levelUpRewardManager;
        [SerializeField] private LevelUpContainer levelUpCocktailContainer;
        [SerializeField] private LevelUpContainer levelUpFunctionContainer;
        [SerializeField] private GameObject levelUpObject;
        [SerializeField] private TextMeshProUGUI levelText;
        
        private readonly List<List<Vector2>> _objectPositionGroups = new();

        private void Awake()
        {
            levelUpRewardManager.OnCockTailAdd += HandleCockTailAdd;
            levelUpRewardManager.OnObjectAddCameraMove += HandleObjectAddCameraMove;
            levelUpRewardManager.OnFuncAdd += HandleFuncAdd;
        }

        private void OnDestroy()
        {
            levelUpRewardManager.OnCockTailAdd -= HandleCockTailAdd;
            levelUpRewardManager.OnObjectAddCameraMove -= HandleObjectAddCameraMove;
            levelUpRewardManager.OnFuncAdd -= HandleFuncAdd;
        }

        private void HandleFuncAdd(Sprite obj)
        {
            ShowUI();
            levelUpFunctionContainer.SpawnSlotSprite(obj);
        }

        private void HandleObjectAddCameraMove(List<Vector2> objectPositions)
        {
            if (objectPositions == null || objectPositions.Count <= 0)
                return;
            
            _objectPositionGroups.Add(new List<Vector2>(objectPositions));
        }

        private void HandleCockTailAdd(int level, CocktailRecipeSO cockTailSo)
        {
            ShowUI();
            levelText.text = level.ToString();
            levelUpCocktailContainer.SpawnSlotCockTail(cockTailSo);
        }

        public void ExitBtn()
        {
            levelUpObject.SetActive(false);
            
            foreach (List<Vector2> objectPositions in _objectPositionGroups)
            {
                cameraManagerSO.RaiseEvent(
                    new CameraManagerEvent().Init(new List<Vector2>(objectPositions))
                );
            }

            _objectPositionGroups.Clear();
        }

        [ContextMenu("Show UI")]
        public void ShowUI()
        {
            if (levelUpObject.activeSelf)
                return;
            
            levelUpObject.SetActive(true);
        }
    }
}