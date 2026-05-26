using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.UI.AchievementUI.Data;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas;
using BBJ.EventSystem;
using BBJ.Staff;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI.UnLockRewards
{
    [CreateAssetMenu(fileName = "unlockCharacter", menuName = "SO/unlock/Character", order = 0)]
    public class UnlockCharacterSO : AbstractUnlockSO
    {
        [Header("Events")]
        [SerializeField] private EventChannelSO cameraManagerEvent;
        [SerializeField] private EventChannelSO achievementEvent;
        [SerializeField] private EventChannelSO unlockUpgradeEvent;
        
        [SerializeField] private AchievementType achievementType;
        [SerializeField] private ButtonType buttonType;
        
        [SerializeField] private Sprite characterSprite; 
        [SerializeField] private StaffConfigSO _staffConfig;
        [SerializeField] private string description;
        
        public readonly List<Vector2> spawnPositions = new List<Vector2>();
        public override void LevelUpReward()
        {
            spawnPositions.Clear();
            achievementEvent.RaiseEvent(new AchievementEvent().Init(achievementType, 1));
            unlockUpgradeEvent.RaiseEvent(new UpgradeUnLockEvent().Init(buttonType));
            Debug.Log(buttonType);
            eventChannelSo?.RaiseEvent(new StaffSpawnEvent().Init(_staffConfig, (pos) => spawnPositions.Add(pos)));
            cameraManagerEvent.RaiseEvent(new CameraManagerEvent().Init(spawnPositions, false));
        }

        public override List<Sprite> GetSprite()
        {
            List<Sprite> sprites = new List<Sprite>();
            sprites.Add(characterSprite);
            return  sprites;
        }

        public override List<string> GetDescription()
        {
            List<string> strings = new List<string>();
            strings.Add(description);
            return strings;
        }
    }
}