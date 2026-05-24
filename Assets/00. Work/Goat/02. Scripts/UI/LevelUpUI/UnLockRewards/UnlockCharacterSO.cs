using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.Events;
using BBJ.EventSystem;
using BBJ.Staff;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI.UnLockRewards
{
    [CreateAssetMenu(fileName = "unlockCharacter", menuName = "SO/unlock/Character", order = 0)]
    public class UnlockCharacterSO : AbstractUnlockSO
    {
        [SerializeField] private EventChannelSO cameraManagerEvent;
        [SerializeField] private Sprite characterSprite; 
        [SerializeField]private StaffConfigSO _staffConfig;
        
        public readonly List<Vector2> spawnPositions = new List<Vector2>();
        public override void LevelUpReward()
        {
            spawnPositions.Clear();
            eventChannelSo?.RaiseEvent(new StaffSpawnEvent().Init(_staffConfig, (pos) => spawnPositions.Add(pos)));
            cameraManagerEvent.RaiseEvent(new CameraManagerEvent().Init(spawnPositions, false));
        }

        public override List<Sprite> GetSprite()
        {
            List<Sprite> sprites = new List<Sprite>();
            sprites.Add(characterSprite);
            return  sprites;
        }
    }
}